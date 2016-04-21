using System;
using System.Collections.Generic;
using System.Linq;

namespace NPoco
{
    public static class Snapshotter
    {
        public static Snapshot<T> StartSnapshot<T>(this IDatabase d, T obj)
        {
            return new Snapshot<T>(d.PocoDataFactory.ForType(obj.GetType()), obj);
        }

        public static int Update<T>(this IDatabase d, T obj, Snapshot<T> snapshot)
        {
            return d.Update(obj, snapshot.UpdatedColumns());
        }
    }

    public class Snapshot<T>
    {
        private readonly PocoData _pocoData;
        private T _trackedObject;
        private readonly Dictionary<PocoColumn, object> _originalValues = new Dictionary<PocoColumn, object>();
        private static IColumnSerializer serializer = new FastJsonColumnSerializer();

        public Snapshot(PocoData pocoData, T trackedObject)
        {
            _pocoData = pocoData;
            _trackedObject = trackedObject;
            PopulateValues(trackedObject);
        }

        private void PopulateValues(T original)
        {
            var clone = original.Copy();
            foreach (var pocoColumn in _pocoData.Columns.Values)
            {
                _originalValues[pocoColumn] = pocoColumn.GetColumnValue(_pocoData, clone);
            }
        }

        public void OverrideTrackedObject(T obj)
        {
            _trackedObject = obj;
        }

        public List<string> UpdatedColumns()
        {
            return Changes().Select(x => x.ColumnName).ToList();
        }

        public class Change
        {
            public string Name { get; set; }
            public string ColumnName { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }
        }

        public List<Change> Changes()
        {
            var list = new List<Change>();
            foreach (var pocoColumn in _originalValues)
            {
                var newValue = pocoColumn.Key.GetColumnValue(_pocoData, _trackedObject);
                if (!AreEqual(pocoColumn.Value, newValue))
                {
                    list.Add(new Change()
                    {
                        Name = pocoColumn.Key.MemberInfoData.Name,
                        ColumnName = pocoColumn.Key.ColumnName,
                        NewValue = newValue,
                        OldValue = pocoColumn.Value
                    });
                }
            }
            return list;
        }

        private static bool AreEqual(object first, object second)
        {
            if (first == null && second == null) return true;
            if (first == null) return false;
            if (second == null) return false;

            var type = first.GetType();
            if (type.IsAClass() || type.IsArray)
            {
                return serializer.Serialize(first) == serializer.Serialize(second);
            }

            return first.Equals(second);
        }
    }
}
