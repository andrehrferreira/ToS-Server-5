/*
 * AbstractTest
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

public abstract class AbstractTest
{
    protected void Describe(string description, Action suite)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"- {description}");
        Console.ResetColor();
        suite();
    }

    protected bool It(string description, Action test)
    {
        try
        {
            test();
            /*Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[PASS] {description}");
            Console.ResetColor();*/
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {description}: {ex.Message}");
            Console.WriteLine(ex);
            Console.ResetColor();
            throw new Exception("");
        }
    }

    protected Expectation<T> Expect<T>(T actual)
    {
        return new Expectation<T>(actual);
    }
}

public class Expectation<T>
{
    private readonly T _actual;

    public Expectation(T actual)
    {
        _actual = actual;
    }

    public void ToBe(T expected)
    {
        if (!Equals(_actual, expected))
        {
            throw new Exception($"Expected: {expected}, but got: {_actual}");
        }
    }

    public void NotToBe(T unexpected)
    {
        if (Equals(_actual, unexpected))
        {
            throw new Exception($"Did not expect: {unexpected}, but got: {_actual}");
        }
    }

    public void NotToBeNull()
    {
        if (_actual == null)
        {
            throw new Exception("Expected value to not be null, but it was null.");
        }
    }

    public void ToBeNull()
    {
        if (_actual != null)
        {
            throw new Exception($"Expected: Null, but got: {_actual}");
        }
    }

    public void ToBeTrue()
    {
        if (!(_actual is bool boolean) || !boolean)
        {
            throw new Exception($"Expected value to be true, but got: {_actual}");
        }
    }

    public void ToBeFalse()
    {
        if (!(_actual is bool boolean) || boolean)
        {
            throw new Exception($"Expected value to be false, but got: {_actual}");
        }
    }

    public void ToContain<U>(U item)
    {
        if (_actual is IEnumerable<U> collection)
        {
            if (!collection.Contains(item))
            {
                throw new Exception($"Expected collection to contain: {item}, but it did not.");
            }
        }
        else
        {
            throw new Exception("The actual value is not a collection that supports containment.");
        }
    }

    public void ToBeGreaterThan<U>(U value) where U : IComparable
    {
        if (_actual is not IComparable comparable)
        {
            throw new Exception($"The actual value does not support comparison.");
        }

        if (comparable.CompareTo(value) <= 0)
        {
            throw new Exception($"Expected value to be greater than: {value}, but got: {_actual}");
        }
    }

    public void ToBeLessThan<U>(U value) where U : IComparable
    {
        if (_actual is not IComparable comparable)
        {
            throw new Exception($"The actual value does not support comparison.");
        }

        if (comparable.CompareTo(value) >= 0)
        {
            throw new Exception($"Expected value to be less than: {value}, but got: {_actual}");
        }
    }

    public void ToBeApproximately(float expected, float tolerance = 0.0001f)
    {
        if (_actual is not float actualFloat)
        {
            throw new Exception("The actual value is not a float and cannot be compared using ToBeApproximately.");
        }

        if (Math.Abs(actualFloat - expected) > tolerance)
        {
            throw new Exception($"Expected: approximately {expected} with tolerance {tolerance}, but got: {actualFloat}");
        }
    }

    public void ToBeApproximately(double expected, double tolerance = 0.0001)
    {
        if (_actual is not double actualDouble)
        {
            throw new Exception("The actual value is not a double and cannot be compared using ToBeApproximately.");
        }

        if (Math.Abs(actualDouble - expected) > tolerance)
        {
            throw new Exception($"Expected: approximately {expected} with tolerance {tolerance}, but got: {actualDouble}");
        }
    }

    public void ToBeGreaterThanOrEqualTo<U>(U value) where U : IComparable
    {
        if (_actual is not IComparable comparable)
        {
            throw new Exception($"The actual value does not support comparison.");
        }

        if (comparable.CompareTo(value) < 0)
        {
            throw new Exception($"Expected value to be greater than or equal to: {value}, but got: {_actual}");
        }
    }

    public void ToBeLessThanOrEqualTo<U>(U value) where U : IComparable
    {
        if (_actual is not IComparable comparable)
        {
            throw new Exception($"The actual value does not support comparison.");
        }

        if (comparable.CompareTo(value) > 0)
        {
            throw new Exception($"Expected value to be less than or equal to: {value}, but got: {_actual}");
        }
    }
}