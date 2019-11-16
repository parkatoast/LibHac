﻿#if NETCOREAPP
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace LibHac.Crypto2
{
    [StructLayout(LayoutKind.Sequential, Size = RoundKeyCount * RoundKeySize)]
    public struct AesCoreNi
    {
        private const int RoundKeyCount = 11;
        private const int RoundKeySize = 0x10;

        private Vector128<byte> _roundKeys;

        public AesCoreNi(ReadOnlySpan<byte> key, bool isDecrypting)
        {
            _roundKeys = default;

            KeyExpansion(key, MemoryMarshal.CreateSpan(ref _roundKeys, RoundKeyCount), isDecrypting);
        }

        public void Initialize(ReadOnlySpan<byte> key, bool isDecrypting)
        {
            KeyExpansion(key, MemoryMarshal.CreateSpan(ref _roundKeys, RoundKeyCount), isDecrypting);
        }

        public readonly ReadOnlySpan<Vector128<byte>> RoundKeys =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _roundKeys), RoundKeyCount);


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public readonly void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int blockCount = Math.Min(input.Length, output.Length) >> 4;

            ref Vector128<byte> inBlock = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(input));
            ref Vector128<byte> outBlock = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(output));

            for (int i = 0; i < blockCount; i++)
            {
                outBlock = EncryptBlock(inBlock);

                inBlock = ref Unsafe.Add(ref inBlock, 1);
                outBlock = ref Unsafe.Add(ref outBlock, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public readonly void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int blockCount = Math.Min(input.Length, output.Length) >> 4;

            ref Vector128<byte> inBlock = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(input));
            ref Vector128<byte> outBlock = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(output));

            for (int i = 0; i < blockCount; i++)
            {
                outBlock = DecryptBlock(inBlock);

                inBlock = ref Unsafe.Add(ref inBlock, 1);
                outBlock = ref Unsafe.Add(ref outBlock, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector128<byte> EncryptBlock(Vector128<byte> input)
        {
            ReadOnlySpan<Vector128<byte>> keys = RoundKeys;

            Vector128<byte> b = Sse2.Xor(input, keys[0]);
            b = Aes.Encrypt(b, keys[1]);
            b = Aes.Encrypt(b, keys[2]);
            b = Aes.Encrypt(b, keys[3]);
            b = Aes.Encrypt(b, keys[4]);
            b = Aes.Encrypt(b, keys[5]);
            b = Aes.Encrypt(b, keys[6]);
            b = Aes.Encrypt(b, keys[7]);
            b = Aes.Encrypt(b, keys[8]);
            b = Aes.Encrypt(b, keys[9]);
            return Aes.EncryptLast(b, keys[10]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector128<byte> DecryptBlock(Vector128<byte> input)
        {
            ReadOnlySpan<Vector128<byte>> keys = RoundKeys;

            Vector128<byte> b = Sse2.Xor(input, keys[10]);
            b = Aes.Decrypt(b, keys[9]);
            b = Aes.Decrypt(b, keys[8]);
            b = Aes.Decrypt(b, keys[7]);
            b = Aes.Decrypt(b, keys[6]);
            b = Aes.Decrypt(b, keys[5]);
            b = Aes.Decrypt(b, keys[4]);
            b = Aes.Decrypt(b, keys[3]);
            b = Aes.Decrypt(b, keys[2]);
            b = Aes.Decrypt(b, keys[1]);
            return Aes.DecryptLast(b, keys[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void KeyExpansion(ReadOnlySpan<byte> key, Span<Vector128<byte>> roundKeys, bool isDecrypting)
        {
            roundKeys[0] = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(key));

            MakeRoundKey(roundKeys, 1, 0x01);
            MakeRoundKey(roundKeys, 2, 0x02);
            MakeRoundKey(roundKeys, 3, 0x04);
            MakeRoundKey(roundKeys, 4, 0x08);
            MakeRoundKey(roundKeys, 5, 0x10);
            MakeRoundKey(roundKeys, 6, 0x20);
            MakeRoundKey(roundKeys, 7, 0x40);
            MakeRoundKey(roundKeys, 8, 0x80);
            MakeRoundKey(roundKeys, 9, 0x1b);
            MakeRoundKey(roundKeys, 10, 0x36);

            if (isDecrypting)
            {
                for (int i = 1; i < 10; i++)
                {
                    roundKeys[i] = Aes.InverseMixColumns(roundKeys[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void MakeRoundKey(Span<Vector128<byte>> keys, int i, byte rcon)
        {
            Vector128<byte> s = keys[i - 1];
            Vector128<byte> t = keys[i - 1];

            t = Aes.KeygenAssist(t, rcon);
            t = Sse2.Shuffle(t.AsUInt32(), 0xFF).AsByte();

            s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 4));
            s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 8));

            keys[i] = Sse2.Xor(s, t);
        }
    }
}
#endif
