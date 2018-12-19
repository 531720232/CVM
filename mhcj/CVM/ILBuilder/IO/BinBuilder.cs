using System;
using System.Collections.Generic;

namespace CVM
{
    public class BinBuilder
    {
        public BinBuilder()
            {
            uints = new Stack<uint>();
            ints = new Stack<int>();
            longs = new Stack<long>();
            bools = new Stack<bool>();
            sbytes = new Stack<sbyte>();
            bytes = new Stack<byte>();

            }
        public int Count { get {

                int count = 0;
                count = uints.Count + ints.Count;
                return count;
                    } }
        public Stack<UInt32> uints;
        public Stack<Int32> ints;
        public Stack<long> longs;
        public Stack<bool> bools;
        public Stack<sbyte> sbytes;
        public Stack<byte> bytes;

        public void WriteBoolean(bool i)
        {
            bools.Push(i);
        //    ints.Push(i);
        }
        public void WriteSByte(sbyte byte1)
        {
            sbytes.Push(byte1);
        }
        public void WriteByte(byte byte1)
        {
            bytes.Push(byte1);
        }
        public void WriteInt64(long i)
        {
            longs.Push(i);
        }
        public void WriteInt32(int i)
        {
            ints.Push(i);
        }
        public void WriteUInt32(uint i)
        {
            uints.Push(i);
        }
        public void Dispose()
        {
            uints.Clear();
            ints.Clear();
            uints = null;
            ints = null;
        }
        public void WriteContentTo(BinBuilder bin)
        {
            bin.ints = this.ints;
            bin.uints = this.uints;
            bin.longs = longs;
            bin.sbytes = sbytes;
            bin.bytes = bytes;
            bin.bools = bools;
        }
    }
}
