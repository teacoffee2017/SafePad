﻿/**
* Safe Pad, a double encrypted note pad that uses 2 passwords to protect your documents and help you keep your privacy.
* 
* Copyright (C) 2016 Stephen Haunts
* http://www.stephenhaunts.com
* 
* This file is part of Safe Pad.
* 
* Safe Pad is free software: you can redistribute it and/or modify it under the terms of the
* GNU General Public License as published by the Free Software Foundation, either version 2 of the
* License, or (at your option) any later version.
* 
* Safe Pad is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* 
* See the GNU General Public License for more details <http://www.gnu.org/licenses/>.
* 
* Authors: Stephen Haunts
*/

using System;
using HauntedHouseSoftware.SecureNotePad.CryptoProviders;

namespace HauntedHouseSoftware.SecureNotePad.DomainObjects.FileFormat
{
    public class FileLoaderBase
    {
        protected IAes Aes;
        protected ISecureHash SecureHash;
        protected IPassword Password;
        protected ICompression Compression;

        public FileLoaderBase(IPassword password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            Aes = new Aes();
            SecureHash = new SecureHash();
            Password = password;
            Compression = new GZipCompression();
        }

        public byte[] Load(byte[] byteStream, int workFactor)
        {
            if (byteStream == null)
            {
                throw new ArgumentNullException(nameof(byteStream));
            }

            var versionNumber = ByteHelpers.CreateSpecialByteArray(2);
            var hash = ByteHelpers.CreateSpecialByteArray(32);
            var salt = ByteHelpers.CreateSpecialByteArray(32);
            var encrypted = ByteHelpers.CreateSpecialByteArray((byteStream.Length - 66));

            SplitFileIntoChunks(byteStream, versionNumber, hash, salt, encrypted);
            CheckFileIntegrity(hash, encrypted);

            return DecryptData(encrypted, salt, workFactor);
        }

        private void CheckFileIntegrity(byte[] hash, byte[] encrypted)
        {
            var computedhash = SecureHash.ComputeHash(encrypted);

            if (!ByteHelpers.ByteArrayCompare(computedhash, hash))
            {
                throw new InvalidOperationException("The signature of the file does not match.");
            }
        }

        private byte[] DecryptData(byte[] encrypted, byte[] salt, int workFactor)
        {

            var decrypted = Aes.Decrypt(encrypted, Convert.ToBase64String(Password.CombinedPasswords), salt, workFactor);

            var decompressed = Compression.Decompress(decrypted);

            return decompressed;
        }

        private static void SplitFileIntoChunks(byte[] buffer, byte[] versionNumber, byte[] hash, byte[] salt, byte[] encrypted)
        {
            int offset = 0;
            Buffer.BlockCopy(buffer, offset, versionNumber, 0, 2);
            offset += 2;

            Buffer.BlockCopy(buffer, offset, salt, 0, 32);
            offset += 32;

            Buffer.BlockCopy(buffer, offset, hash, 0, 32);
            offset += 32;

            Buffer.BlockCopy(buffer, offset, encrypted, 0, encrypted.Length);
        }
    }
}
