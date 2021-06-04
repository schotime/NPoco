using System;

namespace NPoco
{
    /// <summary>
    /// Use to decorate columns, usually of lists, that have a chance of looping on themselves while being mapped
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DepthLimitAttribute : Attribute
    {
        /// <summary>
        /// Default constructor sets the depth limit to 1, which is - do not map beyond one iteration.
        /// </summary>
        public DepthLimitAttribute() 
        {
            DepthLimit = 1;
        }
        public DepthLimitAttribute(int maxDepth) 
        {
            if (maxDepth < 1) throw new ArgumentOutOfRangeException("maxDepth", "Hard depth limit should be at least equal to 1");
            DepthLimit = maxDepth; 
        }
        public int DepthLimit { get; set; }
    }
}