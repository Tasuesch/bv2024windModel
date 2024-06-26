﻿using System;

namespace ClipperLib
{

    internal struct Int128
    {
        private Int64 hi;
        private UInt64 lo;

        public Int128(Int64 _lo)
        {
            lo = (UInt64) _lo;
            if (_lo < 0)
            {
                hi = -1;
            }
            else
            {
                hi = 0;
            }
        }

        public Int128(Int64 _hi, UInt64 _lo)
        {
            lo = _lo;
            hi = _hi;
        }

        public Int128(Int128 val)
        {
            hi = val.hi;
            lo = val.lo;
        }

        public bool IsNegative()
        {
            return hi < 0;
        }

        public static bool operator ==(Int128 val1, Int128 val2)
        {
            if ((object) val1 == (object) val2)
            {
                return true;
            }
            if ((object) val1 == null || (object) val2 == null)
            {
                return false;
            }
            return (val1.hi == val2.hi && val1.lo == val2.lo);
        }

        public static bool operator !=(Int128 val1, Int128 val2)
        {
            return !(val1 == val2);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is Int128))
            {
                return false;
            }
            var i128 = (Int128) obj;
            return (i128.hi == hi && i128.lo == lo);
        }

        public override int GetHashCode()
        {
            return hi.GetHashCode() ^ lo.GetHashCode();
        }

        public static bool operator >(Int128 val1, Int128 val2)
        {
            if (val1.hi != val2.hi)
            {
                return val1.hi > val2.hi;
            }
            return val1.lo > val2.lo;
        }

        public static bool operator <(Int128 val1, Int128 val2)
        {
            if (val1.hi != val2.hi)
            {
                return val1.hi < val2.hi;
            }
            return val1.lo < val2.lo;
        }

        public static Int128 operator +(Int128 lhs, Int128 rhs)
        {
            lhs.hi += rhs.hi;
            lhs.lo += rhs.lo;
            if (lhs.lo < rhs.lo)
            {
                lhs.hi++;
            }
            return lhs;
        }

        public static Int128 operator -(Int128 lhs, Int128 rhs)
        {
            return lhs + -rhs;
        }

        public static Int128 operator -(Int128 val)
        {
            if (val.lo == 0)
            {
                return new Int128(-val.hi, 0);
            }
            return new Int128(~val.hi, ~val.lo + 1);
        }

        public static explicit operator double(Int128 val)
        {
            const double shift64 = 18446744073709551616.0; //2^64
            if (val.hi < 0)
            {
                if (val.lo == 0)
                {
                    return (double) val.hi * shift64;
                }
                return -(double) (~val.lo + ~val.hi * shift64);
            }
            return (double) (val.lo + val.hi * shift64);
        }

        //nb: Constructing two new Int128 objects every time we want to multiply longs  
        //is slow. So, although calling the Int128Mul method doesn't look as clean, the 
        //code runs significantly faster than if we'd used the * operator.

        public static Int128 Int128Mul(Int64 lhs, Int64 rhs)
        {
            var negate = (lhs < 0) != (rhs < 0);
            if (lhs < 0)
            {
                lhs = -lhs;
            }
            if (rhs < 0)
            {
                rhs = -rhs;
            }
            var int1Hi = (UInt64) lhs >> 32;
            var int1Lo = (UInt64) lhs & 0xFFFFFFFF;
            var int2Hi = (UInt64) rhs >> 32;
            var int2Lo = (UInt64) rhs & 0xFFFFFFFF;

            //nb: see comments in clipper.pas
            var a = int1Hi * int2Hi;
            var b = int1Lo * int2Lo;
            var c = int1Hi * int2Lo + int1Lo * int2Hi;

            UInt64 lo;
            Int64 hi;
            hi = (Int64) (a + (c >> 32));

            unchecked
            {
                lo = (c << 32) + b;
            }
            if (lo < b)
            {
                hi++;
            }
            var result = new Int128(hi, lo);
            return negate ? -result : result;
        }

    };

}