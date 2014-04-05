using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IdentityColumnAttribute : ColumnAttribute
    {
        private int _seed;
        private int _increment;

        public IdentityColumnAttribute()
        {
            DefaultValues();
        }

        public IdentityColumnAttribute(string name) : base(name)
        {
            DefaultValues();
        }

        public IdentityColumnAttribute(int Seed, int Increment)
        {
            _seed = Seed;
            _increment = Increment;
        }

        public IdentityColumnAttribute(string name, int Seed, int Increment) : base(name)
        {
            _seed = Seed;
            _increment = Increment;
        }

        private void DefaultValues()
        {
            _seed = 0;
            _increment = 1;
        }

        public int Seed
        {
            get { return _seed; }
        }

        public int Increment
        {
            get { return _increment; }
        }

    }
}