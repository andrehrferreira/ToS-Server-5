/*
 * Base36
 * 
 * Author: Andre Ferreira
 * 
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 *    
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
