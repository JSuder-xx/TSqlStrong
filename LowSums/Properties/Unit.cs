using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Containers
{
    public class Unit
    {
        public readonly static Unit unit = new Unit();
        protected Unit() { }

        public override bool Equals(object obj)
        {
            return (obj is Unit) ? true : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
