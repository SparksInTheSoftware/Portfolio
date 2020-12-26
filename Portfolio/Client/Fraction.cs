using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Portfolio.Client
    {
    public struct Fraction
        {
        public int numerator;
        public int denominator;

        public Fraction(int numerator, int denominator)
            {
            this.numerator = numerator;
            this.denominator = denominator;
            }

        public override string ToString()
            {
            return $"{this.numerator}/{this.denominator}";
            }

        public static Point operator * (Point left, Fraction right)
            {
            return new(left.X * right, left.Y * right);
            }

        public static Size operator * (Size left, Fraction right)
            {
            return new(left.Width * right, left.Height * right);
            }

        public static int operator * (Fraction left, int right)
            {
            return left.numerator * right / left.denominator;
            }

        public static int operator * (int right, Fraction left)
            {
            return left * right;
            }

        public static int operator / (int right, Fraction left)
            {
            return left.denominator * right / left.numerator;
            }

        public static explicit operator double (Fraction f)
            {
            return ((double)f.numerator) / ((double)f.denominator);
            }
        }
    }
