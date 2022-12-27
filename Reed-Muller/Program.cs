using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reed_Muller
{

    internal class Program
    {
        
        static void Main(string[] args)
        {
            //var message = @"F";
            //Console.WriteLine($"Исходное сообщение: {message}\n");
            //var data = ReedMuller.EncodeMessage(message);

            //ReedMuller.CreateErrors(ref data);

            //Console.WriteLine("Исходное сообщение: " + ReedMuller.DecodeMessage(data));

            var vectors = ReedMuller.EncodeMessage("Тестовый текст");
            var bytes = ReedMuller.ConvertToBytes(vectors);
            var newv = ReedMuller.ConvertVectors(bytes, 128);
            var mes = ReedMuller.DecodeMessage(newv);
            Console.WriteLine(mes);

        }

        
    }
}
