// nanobip39
// Copyright (C) 2021 Michael McMaster <michael@codesrc.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace nanobip39
{
    class Program
    {
        // Convert a LSB BitArray to MSB
        // It's not fast, but it works
        static void FlipBitOrder(BitArray bitArray)
        {
            for (var i = 0; i < bitArray.Length; i += 8)
            {
                for (var j = 0; j < 4; ++j)
                {
                    var tmp = bitArray[i + j];
                    bitArray[i + j] = bitArray[i + (7 - j)];
                    bitArray[i + (7 - j)] = tmp;
                }
            }
        }

        static BitArray Checksum(byte[] entropy)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(entropy);
                var result = new BitArray(hash);

                FlipBitOrder(result);

                // Only need first few bits
                result.Length = entropy.Length * 8 / 32;
                return result;
            }
        }

        static IEnumerable<int> Generate(int bits)
        {
            // Uses the Cryptographic random number generator
            using (var rng = RandomNumberGenerator.Create())
            {
                // Grab some random numbers
                var entropy = new byte[bits / 8];
                rng.GetBytes(entropy);

                // Append a few bits of checksum to the end
                var checksum = Checksum(entropy);
                var seedBits = new BitArray(entropy);
                FlipBitOrder(seedBits);
                seedBits.Length = seedBits.Length + checksum.Length;
                for (var idx = 0; idx < checksum.Length; ++ idx)
                {
                    seedBits.Set(idx + entropy.Length * 8, checksum.Get(idx));
                }

                // Use 11-bit indexes into the word table
                for (var bitIndex = 0; bitIndex < seedBits.Length; bitIndex += 11)
                {
                    var wordIdx = 0;
                    for (var i = 0; i < 11; i++)
                    {
                        wordIdx = wordIdx + ((seedBits.Get(bitIndex + i) ? 1 : 0) << (10 - i));
                    }

                    yield return wordIdx;
                }
            }

        }

        static void Main(string[] args)
        {
            var wordLength = 12;

            if (args.Length != 2 ||
                !int.TryParse(args[0], out wordLength) ||
                (wordLength != 12 && wordLength != 24) ||
                !File.Exists(args[1]))
            {
                Console.Error.WriteLine("Usage: dotnet run words /path/to/wordlist");
                Console.Error.WriteLine("eg:");
                Console.Error.WriteLine("\tdotnet run 24 english.txt");
                System.Environment.Exit(1);
            }

            var wordList = File.ReadLines(args[1]).ToList();

            foreach (var wordIndex in Generate(wordLength == 12 ? 128 : 256))
            {
                Console.WriteLine(wordList[wordIndex]);
            }
        }
    }
}
