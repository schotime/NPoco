using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace NPoco
{
    public class PocoMember
    {
        public PocoMember()
        {
            PocoMemberChildren = new List<PocoMember>();
            ReferenceType = ReferenceType.None;
        }

        public string Name
        {
            get
            {
                return MemberInfoData.Name;
            }
        }

        public MemberInfoData MemberInfoData
        {
            get;
            set;
        }

        public PocoColumn PocoColumn
        {
            get;
            set;
        }

        public List<PocoMember> PocoMemberChildren
        {
            get;
            set;
        }

        public ReferenceType ReferenceType
        {
            get;
            set;
        }

        public string ReferenceMemberName
        {
            get;
            set;
        }

        public bool IsList
        {
            get;
            set;
        }

        public bool IsDynamic
        {
            get;
            set;
        }

        public List<MemberInfo> MemberInfoChain
        {
            get;
            set;
        }

        private FastCreate _creator;
        private MemberAccessor _memberAccessor;
        private Type _listType;

        public virtual object Create(DbDataReader dataReader)
        {
            return _creator.Create(dataReader);
        }

        public IList CreateList()
        {
            var list = Activator.CreateInstance(_listType);
            return (IList)list;
        }

        public void SetMemberAccessor(MemberAccessor memberAccessor, FastCreate fastCreate, Type listType)
        {
            _listType = listType;
            _memberAccessor = memberAccessor;
            _creator = fastCreate;
        }

        public virtual void SetValue(object target, object value)
        {
            _memberAccessor.Set(target, value);
        }

        public virtual object GetValue(object target)
        {
            return _memberAccessor.Get(target);
        }
    }
}