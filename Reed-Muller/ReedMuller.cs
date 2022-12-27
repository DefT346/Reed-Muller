using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reed_Muller
{
    public static class ReedMuller
    {
        static Vector<float> CreateBitVector(int value, int m)
        {
            var parsed = Convert.ToString(value, 2).PadLeft(m + 1, '0');
            return Vector<float>.Build
                .Dense(parsed.Select(e => e == '1' ? 1f : 0f)
                .ToArray());
        }

        public static void CreateErrors(ref List<Vector<float>> data)
        {
            Console.WriteLine("--- Внесение ошибок\n");
            var random = new Random();
            int counter = 0;
            foreach (var vector in data)
            {
                //int count = new Random().Next(2, 5);
                Console.WriteLine($"[{counter}] Вектор без/c ошибками: \n{RowVector(vector, false)}");


                for (int i = 0; i < 5; i++)
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

        public static List<Vector<float>> ConvertVectors(byte[] bytes, int vectorsize)
        {
            var list = new List<Vector<float>>();
            var s = vectorsize / 8;
            for (int i = 0; i < bytes.Length / s; i++)
            {
                list.Add(ConvertToVector(ref bytes, i * s, vectorsize));
            }
            return list;
        }

        public static Vector<float> ConvertToVector(ref byte[] bytes, int offset, int vectorsize)
        {
            var vector = Vector<float>.Build.Dense(vectorsize);
            for(int i = 0; i < vectorsize; i++)
            {
                var index = offset + i / 8;
                var cur = bytes[index];
                var bit = ((int)cur).GetBit(i % 8);
                vector[i] = bit;
            }
            return vector;
        }

        public static byte[] ConvertToBytes(List<Vector<float>> data)
        {
            List<byte> bytes = new List<byte>();
            foreach (var vector in data)
            {
                bytes.AddRange(ConvertToBytes(vector));
            }
            return bytes.ToArray();

            //var buffer = new Lis
            //for(int i =0; i< )
            //{
            //    ConvertToByte(vector);
            //}
        }

        public static List<byte> ConvertToBytes(Vector<float> vector)
        {
            //if (vector.Count > 8)
            //    throw new ArgumentException("ConvertToByte can only work with a BitArray containing a maximum of 8 values");

            List<byte> bytes = new List<byte>();

            var vectorIndex = 0;
            for (byte n = 0; n < vector.Count; n += 8)
            {
                byte result = 0;
                for (byte i = 0; i < 8; i++)
                {
                    if (vector[vectorIndex] == 1)
                        result |= (byte)(1 << i);

                    vectorIndex++;
                }
                bytes.Add(result);
            }

            return bytes;
        }

        public static List<Vector<float>> EncodeBytes(byte[] bytes, int m = 7, Action<float> progress = default)
        {
            var vectors = new List<Vector<float>>();
            int p = 0;
            for (int i = 0; i < bytes.Count(); i++)
            {
                var v = EncodeByte(bytes[i]);
                vectors.Add(v);
                p++;
                progress?.Invoke(p / (float)bytes.Count());
            }
            return vectors;
        }

        public static byte[] DecodeBytes(List<Vector<float>> vectors, Action<float> progress = default)
        {
            var Hm = Adamar(H1);
            var buffer = new List<byte>();
            int counter = 0;
            int p = 0;
            foreach (var vector in vectors)
            {
                buffer.Add(Decode(vector, Hm));
                counter++;
                p++;
                progress?.Invoke(p / (float)vectors.Count);
            }

            return buffer.ToArray();
        }

        public static List<Vector<float>> EncodeMessage(string messsage, int m = 7, Action<float> progress = default)
        {
            return EncodeBytes(Encoding.UTF8.GetBytes(messsage), m, progress);
        }

        public static string DecodeMessage(List<Vector<float>> vectors, Action<float> progress = default)
        {
            //Console.WriteLine("\n--- Декодирование\n");
            return Encoding.UTF8.GetString(DecodeBytes(vectors, progress));
        }

        public static void Progress(float p)
        {
            if ((p * 100) % 5 < 0.001f) Console.Write($"{(int)(p * 100)}% ");
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
            //Console.WriteLine("Принятый вектор кодового слова с ошибкой Y': \n" + RowVector(Y, false) + "\n");
            var YH = YY * HmAdamar;
            //Console.WriteLine("Умноженный преобразованный вектор Y'H7: \n" + RowVector(YH) + "\n");

            //var index = YH.AbsoluteMaximumIndex();

            var index = 0;
            var component = 0f;
            var max = 0f;
            for (int i = 0; i < YH.Count; i++)
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

            //Console.WriteLine("Исправленное кодовое слово y: \n" + RowVector(y, false) + "\n");


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
            //Console.WriteLine($"Результат ({resultByte}) в бинарном виде: " + Convert.ToString(resultByte, 2).PadLeft(m + 1, '0') + "\n");
            //Console.WriteLine();

            return (byte)resultByte;
        }
    }
}
