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

 #include "Utils/Base36.h"

 int32 UBase36::Base36ToInt(const FString& Value)
 {
     return FCString::Strtoi(*Value, nullptr, 36);
 }

 FString UBase36::IntToBase36(int32 Value)
 {
     FString chars = TEXT("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
     FString result = TEXT("");

     if (Value == 0)
         return TEXT("0");

     while (Value > 0) {
         int remainder = Value % 36;
         Value /= 36;
         result += chars[remainder];
     }

     result = result.Reverse();

     return result;
 }
