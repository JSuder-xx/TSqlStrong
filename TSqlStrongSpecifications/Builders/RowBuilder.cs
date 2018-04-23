using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSqlStrong.TypeSystem;
using TSqlStrong.Symbols;

namespace TSqlStrongSpecifications.Builders
{
    public class RowBuilder
    {
        private CaseSensitivity _caseSensitivity = CaseSensitivity.CaseInsensitive;
        private List<ColumnDataType> _columns = new List<ColumnDataType>();

        public readonly static RowDataType EmptyRow = new RowDataType();

        public static RowBuilder WithAnonymousColumn(DataType dataType)
        {
            return new RowBuilder().AndAnonymousColumn(dataType);
        }

        public static RowBuilder WithSchemaNamedColumn(string name, DataType dataType)
        {
            return new RowBuilder().AndSchemaNamedColumn(name, dataType);
        }

        public static RowBuilder WithAliasedColumn(string name, DataType dataType)
        {
            return new RowBuilder().AndAliasedColumn(name, dataType);
        }

        public RowBuilder AndAnonymousColumn(DataType dataType)
        {
            _columns.Add(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, dataType));
            return this;
        }

        public RowBuilder AndSchemaNamedColumn(string name, DataType dataType)
        {
            _columns.Add(new ColumnDataType(new ColumnDataType.ColumnName.Schema(name, _caseSensitivity), dataType));
            return this;
        }

        public RowBuilder AndAliasedColumn(string name, DataType dataType)
        {
            _columns.Add(new ColumnDataType(new ColumnDataType.ColumnName.Aliased(name, _caseSensitivity), dataType));
            return this;
        }

        public RowDataType CreateRow()
        {
            return new RowDataType(_columns.ToArray());
        }
    }
}
