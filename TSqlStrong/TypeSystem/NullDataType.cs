using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LowSums;
using TSqlStrong.VerificationResults;

namespace TSqlStrong.TypeSystem
{
    /// <summary>
    /// A type inhabited by the single value Null. The evil bottom that subverts assignability checking. 
    /// </summary>
    public class NullDataType : DataType
    {
        public static readonly NullDataType Instance = new NullDataType();

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            (otherType is NullDataType) ? Try.SuccessUnit
            : (otherType is NullableDataType otherAsNullable) ? Try.SuccessUnit
            : Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()));
        
        public override int SizeOfDomain => 1;

        public override string ToString() => "<NULL>";        
    }
}
