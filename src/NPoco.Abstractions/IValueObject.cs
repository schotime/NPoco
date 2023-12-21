using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    /// <summary>
    /// The interface to define value object mapping
    /// </summary>
    /// <typeparam name="TColumnType">Type of the column to map to</typeparam>
    public interface IValueObject<TColumnType> : IValueObject
    {
        TColumnType Value { get; set; }
    }
        
    public interface IValueObject
    {
        
    }
}
