/*
 * Base36
 * 
 * Author: Andre Ferreira
 * 
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 */

namespace Wormhole
{
    public struct Base36
    {
        private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static int ToInt(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("The input string cannot be null or empty.");

            int result = 0;

            foreach (char c in id.ToUpper())
            {
                int digitValue = Digits.IndexOf(c);

                if (digitValue == -1)
                    throw new ArgumentException("Invalid Base36 value.");

                result = result * 36 + digitValue;
            }

            return result;
        }

        public static string ToString(int value)
        {
            if (value < 0)
                throw new ArgumentException("Value cannot be negative.");

            string result = string.Empty;

            do
            {
                result = Digits[value % 36] + result;
                value /= 36;
            } while (value > 0);

            return result;
        }
    }
}
