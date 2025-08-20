/*
 * ChaCha20-Poly1305 wrappers
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

#include "Crypto/ChaCha20Poly1305.h"
#include <sodium.h>

bool UChaCha20Poly1305::ChaCha20Poly1305Ietf_Encrypt(const TArray<uint8>& Key, const TArray<uint8>& Nonce, const TArray<uint8>& AAD, const TArray<uint8>& Plain, TArray<uint8>& Cipher)
{
    if (sodium_init() < 0)
        return false;

    Cipher.SetNumUninitialized(Plain.Num() + crypto_aead_chacha20poly1305_IETF_ABYTES);
    unsigned long long outLen = 0;
    int res = crypto_aead_chacha20poly1305_ietf_encrypt(
        Cipher.GetData(), &outLen,
        Plain.GetData(), Plain.Num(),
        AAD.GetData(), AAD.Num(),
        nullptr,
        Nonce.GetData(), Key.GetData());
    return res == 0;
}

bool UChaCha20Poly1305::ChaCha20Poly1305Ietf_Decrypt(const TArray<uint8>& Key, const TArray<uint8>& Nonce, const TArray<uint8>& AAD, const TArray<uint8>& Cipher, TArray<uint8>& Plain)
{
    if (sodium_init() < 0)
        return false;

    if (Cipher.Num() < crypto_aead_chacha20poly1305_IETF_ABYTES)
        return false;

    Plain.SetNumUninitialized(Cipher.Num() - crypto_aead_chacha20poly1305_IETF_ABYTES);
    unsigned long long outLen = 0;
    int res = crypto_aead_chacha20poly1305_ietf_decrypt(
        Plain.GetData(), &outLen, nullptr,
        Cipher.GetData(), Cipher.Num(),
        AAD.GetData(), AAD.Num(),
        Nonce.GetData(), Key.GetData());
    return res == 0;
}
