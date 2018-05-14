using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;
using TSqlStrong.Symbols;

namespace TSqlStrong.TypeSystem
{
    partial class ColumnDataType
    {
        public static class ColumnName
        {
            public static Base TakeNamed(Base left, Base right) =>
                left is BaseNamedColumn
                    ? left
                    : right;

            /// <summary>
            /// The most base class of column name. 
            /// </summary>
            public abstract class Base
            {
                public virtual ITry<Unit> IsAssignableTo(Base columnName) => 
                    Try.SuccessUnit;

                public abstract bool Matches(string name);                    
            }

            /// <summary>
            /// A completely unnamed column. 
            /// </summary>
            public class Anonymous : Base
            {
                public readonly static Anonymous Instance = new Anonymous();

                protected Anonymous() { }

                public override string ToString() => "_";

                public override bool Matches(string name) => false;

                public override int GetHashCode()
                {
                    return 13;
                }

                public override bool Equals(Object other) =>
                    other is Anonymous ? true : base.Equals(other);
            }

            /// <summary>
            /// A base class for a column that has a name.
            /// </summary>
            public abstract class BaseNamedColumn : Base
            {
                private readonly string _val;
                private readonly CaseSensitivity _caseSensitivity;

                public BaseNamedColumn(string val, CaseSensitivity caseSensitivity)
                {
                    _val = val;
                    _caseSensitivity = caseSensitivity;
                }

                public override bool Matches(string name) => _caseSensitivity.AreEqual(name, Name);

                public string Name => _val;

                public override int GetHashCode()
                {
                    return Name.GetHashCode() * 239 + _caseSensitivity.GetHashCode();
                }

                public override bool Equals(Object other) =>
                    other is BaseNamedColumn otherAsNamed 
                        ? Matches(otherAsNamed.Name)
                        : base.Equals(other);
            }

            /// <summary>
            /// A column that has a name directly from the database schema.
            /// </summary>
            public class Schema : BaseNamedColumn
            {
                public Schema(string val, CaseSensitivity caseSensitivity) : base(val, caseSensitivity) { }

                public override string ToString() => $"Column('{Name}')";
            }

            /// <summary>
            /// A column that has a user provided name. 
            /// </summary>
            public class Aliased : BaseNamedColumn
            {
                public Aliased(string val, CaseSensitivity caseSensitivity) : base(val, caseSensitivity) { }

                public override string ToString() => $"Alias('{Name}')";

                public override ITry<Unit> IsAssignableTo(Base columnName) =>
                    (columnName is BaseNamedColumn named)
                        ? this.Matches(named.Name)
                            ? Try.SuccessUnit
                            : Try.Failure<Unit>(VerificationResults.Messages.CannotAssignColumnName(source: Name, destination: columnName.ToString()))
                        : Try.SuccessUnit;
            }
        }
    }
}
