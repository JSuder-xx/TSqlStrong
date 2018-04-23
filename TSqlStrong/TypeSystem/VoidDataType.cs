using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    public class VoidDataType : DataType
    {
        public static readonly VoidDataType Instance = new VoidDataType();
        public override int SizeOfDomain => 0;
        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) => Try.Failure<Unit>("Cannot assign void to anything.");
        protected override ITry<Unit> OnCanCompareWith(DataType otherType) => Try.Failure<Unit>("Cannot compare void with anything.");
    }
}

