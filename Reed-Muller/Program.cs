using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reed_Muller
{
    static class BitExtensions
    {
        public static int GetBit(this int b, int index) => (b >> index) & 1;

        public static void SetBit(this ref int intValue, int bitPosition, int bit)
        {
            if (bit == 1) intValue |= (1 << bitPosition);
            else intValue &= ~(1 << bitPosition);
        }
    }

    public static class MatrixExtensions
    {
        public static Matrix<float> CInsertColumn(this Matrix<float> matrix, Vector<float> col)
        {
            var newMatrix = Matrix<float>.Build.Dense(matrix.RowCount, matrix.ColumnCount + 1);
            newMatrix.SetSubMatrix(0, 1, matrix);
            newMatrix.SetColumn(0, col);
            return newMatrix;
        }

        public static Matrix<float> CInsertRow(this Matrix<float> matrix, Vector<float> row)
        {
            var newMatrix = Matrix<float>.Build.Dense(matrix.RowCount + 1, matrix.ColumnCount);
            newMatrix.SetSubMatrix(1, 0, matrix);
            newMatrix.SetRow(0, row);
            return newMatrix;
        }
    }

    internal class Program
    {
        
        static Vector<float> CreateBitVector(int value, int m)
        {
            var parsed = Convert.ToString(value, 2).PadLeft(m + 1, '0');
            return Vector<float>.Build
                .Dense(parsed.Select(e => e == '1' ? 1f : 0f)
                .ToArray());
        }

        static void Main(string[] args)
        {
            var message = @"F";
            Console.WriteLine($"Исходное сообщение: {message}\n");
            var data = EncodeMessage(message);

            CreateErrors(ref data);

            Console.WriteLine("Исходное сообщение: " + DecodeMessage(data));

        }

        static void CreateErrors(ref List<Vector<float>> data)
        {
            Console.WriteLine("--- Внесение ошибок\n");
            var random = new Random();
            int counter = 0;
            foreach (var vector in data)
            {
                //int count = new Random().Next(2, 5);
                Console.WriteLine($"[{counter}] Вектор без/c ошибками: \n{RowVector(vector, false)}");
                

                for (int i =0; i < 5; i++)
                {
                    int index = random.Next(0, vector.Count);
                    vector[index] = (vector[index] + 1) % 2;
                }


                Console.WriteLine(RowVector(vector, false));
                counter++;

            }
        }

        static string RowVector(Vector<float> vector, bool limiter = true)
        {
            string row = "";
            foreach (var bit in vector)
            {
                row += (limiter ? " " : "") + (int)bit;
            }
            return row;
        }

        static List<Vector<float>> EncodeMessage(string messsage, int m = 7)
        {
            var vectors = new List<Vector<float>>();
            var bytes = Encoding.UTF8.GetBytes(messsage);
            for (int i = 0; i< bytes.Count(); i++)
            {
                var v = EncodeByte(bytes[i]);
                vectors.Add(v);
                Console.WriteLine($"Бинарный код символа [{messsage[i]}] ({bytes[i]}): " + Convert.ToString(bytes[i], 2).PadLeft(m + 1, '0') + "\n");
            }
            return vectors;
        }

        static string DecodeMessage(List<Vector<float>> vectors)
        {
            Console.WriteLine("\n--- Декодирование\n");
            var Hm = Adamar(H1);
            var buffer = new List<byte>();
            int counter = 0;
            foreach (var vector in vectors)
            {
                Console.WriteLine($"[{counter}]");
                buffer.Add(Decode(vector, Hm));
                counter++;
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        static void Testing()
        {
            int m = 7;
            var Hm = Adamar(H1, m);
            for (int i = 0; i < 256; i++)
            {
                var y = EncodeByte((byte)i, m);
                //Console.WriteLine(Convert.ToString(i, 2).PadLeft(m + 1,'0'));
                var x = Decode(y, Hm, m);
                Console.WriteLine($"{i} -> {x}");
            }       
        }


        static Matrix<float> H1 = Matrix<float>.Build.DenseOfArray(
            new float[,]
            {{ 1f, 1f },
            { 1f, -1f }});

        static Matrix<float> Adamar(Matrix<float> prevMatrix, int m = 7, int n = 2)
        {
            int offset = (int)Math.Pow(2, n - 1);
            int size2 = (int)Math.Pow(2, n);
            var Hn = Matrix<float>.Build.Dense(size2, size2);
            Hn.SetSubMatrix(0, 0, prevMatrix);
            Hn.SetSubMatrix(offset, 0, prevMatrix);
            Hn.SetSubMatrix(0, offset, prevMatrix);
            Hn.SetSubMatrix(offset, offset, prevMatrix.Multiply(-1f));

            n++;

            if (n > m)
                return Hn;
            else
                return Adamar(Hn, m, n);
        }

        static Vector<float> EncodeByte(byte b, int m = 7)
        {
            int cols = (int)Math.Pow(2, m);

            var G1 = Matrix<float>.Build.Dense(m, cols);

            for (int n = 0; n < cols; n++)
                for (int row = 0; row < m; row++)
                    G1[m - row - 1, n] = n.GetBit(row);

            G1 = G1.CInsertRow(Vector<float>.Build.Dense(cols, 1));

            //Console.WriteLine(G1);

            // Кодирование
            var x = CreateBitVector(b, m);
            return (x * G1).Modulus(2f);
        }


        static byte Decode(Vector<float> Y, Matrix<float> HmAdamar, int m = 7)
        {

            // Исправление ошибок
            //Console.WriteLine(HmAdamar);

            var YY = Y.Multiply(2f).Subtract(1f);
            Console.WriteLine("Принятый вектор кодового слова с ошибкой Y': \n" + RowVector(Y, false) + "\n");
            var YH = YY * HmAdamar;
            Console.WriteLine("Умноженный преобразованный вектор Y'H7: \n" + RowVector(YH) + "\n");

            //var index = YH.AbsoluteMaximumIndex();

            var index = 0;
            var component = 0f;
            var max = 0f;
            for (int i =0; i < YH.Count; i++)
            {
                var absEl = Math.Abs(YH[i]);
                if (absEl > max)
                {
                    max = absEl;
                    component = YH[i];
                    index = i;
                }
            }


            Vector<float> y = HmAdamar.Row(index).Add(1f).Divide(2f);
            //Console.WriteLine("Исправленное y = " + y.ToRowMatrix().ToMatrixString());
            if (component < 0)
            {
                y = y.Add(1f).Modulus(2f);
                //Console.WriteLine("(Орицательная компонента) Исправленное y = " + y.ToRowMatrix().ToMatrixString());

            }

            Console.WriteLine("Исправленное кодовое слово y: \n" + RowVector(y, false) + "\n");


            int resultByte = 0;
            //float[] x = new float[m + 1];
            //x[0] = y[0];
            resultByte.SetBit(m, (int)y[0]);
            for (int i = 0; i < m; i++)
            {
                var bit = (int)y[0] ^ (int)y[(int)Math.Pow(2, i)];
                resultByte.SetBit(i, bit);
            }



            //// Декодирование
            //int resultByte = 0;
            //resultByte.SetBit(m, (int)y[0]);
            //for (int i = m - 1; i >= 0; i--)
            //{
            //    int a = (int)Math.Pow(2, i);
            //    var bit = (int)y[a] ^ 1;
            //    resultByte.SetBit(i, bit);
            //}
            Console.WriteLine($"Результат ({resultByte}) в бинарном виде: " + Convert.ToString(resultByte, 2).PadLeft(m + 1,'0') + "\n");
            Console.WriteLine();

            return (byte)resultByte;
        }
    }
}
