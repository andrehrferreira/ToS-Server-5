/*
 * Quantization
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

public static class Quantization
{
    public static short ToShort(float value, float min, float max)
    {
        float clamped = Math.Clamp(value, min, max);
        float normalized = (clamped - min) / (max - min);
        return (short)MathF.Round(normalized * short.MaxValue);
    }

    public static float ToFloat(short value, float min, float max)
    {
        float normalized = value / (float)short.MaxValue;
        return normalized * (max - min) + min;
    }

    public static void ToShort(FVector value, float min, float max, out short x, out short y, out short z)
    {
        x = ToShort(value.X, min, max);
        y = ToShort(value.Y, min, max);
        z = ToShort(value.Z, min, max);
    }

    public static FVector ToVector(short x, short y, short z, float min, float max)
    {
        return new FVector(ToFloat(x, min, max), ToFloat(y, min, max), ToFloat(z, min, max));
    }
}
