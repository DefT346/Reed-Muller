using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reed_Muller
{
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
}
