//--------------------------------------------------------------------------------------------
// Copyright 2015 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing.DataSources;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Processing
{
    /// <summary>
    /// Maintains the hierarchy of objects currently being processed.
    /// The index of the object in a scope is updated relative to all parents
    /// </summary>
    public class ProcessingScope
    {
        /// <summary>
        /// The parent scope if any
        /// </summary>
        public ProcessingScope Parent { get; private set; }

        private readonly Dictionary<ITableMapper, ProcessingScope> _children = new Dictionary<ITableMapper, ProcessingScope>();

        /// <summary>
        /// The object tracked in this scope
        /// </summary>
        public object CurrentObject { get; private set; }

        /// <summary>
        /// The nesting depth of this scope (number of parents)
        /// </summary>
        public int Depth { get; private set; }

        private readonly int[] _indices;


        private readonly Dictionary<TableDataBuilder, Dictionary<object[], bool[]>> _uniqueChildFields =
            new Dictionary<TableDataBuilder, Dictionary<object[], bool[]>>();
        

        /// <summary>
        /// The provider for field values in Sitecore's item database associated with this scope
        /// </summary>
        public IItemFieldLookup FieldLookup { get; set; }

        public ProcessingScope()
            : this(null)
        {

        }

        private ProcessingScope(ProcessingScope parent)
        {
            Depth = parent != null ? parent.Depth + 1 : 0;
            Parent = parent;            
            FieldLookup = parent != null ? parent.FieldLookup : null;
            _indices = new int[Depth + 1];
        }


        /// <summary>
        /// Returns the object of the type specified closest to this scope (including itself), if any
        /// </summary>
        /// <typeparam name="TObject">The type of the object to return</typeparam>
        /// <returns></returns>
        public TObject Current<TObject>() where TObject : class
        {
            var scope = ParentOrSelf<TObject>();

            var value = scope != null ? scope.CurrentObject as TObject : null;

            return value;
        }

        /// <summary>
        /// This absolute index of the item in this scope since processing started
        /// </summary>
        public int GlobalIndex
        {
            get { return Index(null); }
        }

        /// <summary>
        /// The index of the current item relative to the scope's parent
        /// </summary>
        public int ChildIndex
        {
            get { return Index(Parent); }
        }


        void Reset(IDictionary<ITableMapper, ProcessingScope> children)
        {
            foreach (var child in children.Values)
            {
                child._indices[Depth + 1] = 0;
                Reset(child._children);
            }
        }

        /// <summary>
        /// Updates the item in this scope and increments indices
        /// </summary>
        /// <param name="o">The item to set in this scope</param>
        /// <returns></returns>
        public ProcessingScope Set(object o)
        {
            if (o == null) throw new ArgumentNullException("o");

            Reset(_children);


            for (var i = 0; i < _indices.Length; i++)
            {
                ++_indices[i]; //Increment all indices for this scope
            }

            CurrentObject = o;

            //Clear unique fields
            foreach (var dict in _uniqueChildFields.Values)
            {
                dict.Clear();
            }

            return this;
        }

        /// <summary>
        /// Replaces the item in this scope without incrementing indices
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public IDisposable Swap(object o)
        {
            var old = CurrentObject;
            CurrentObject = o;
            return new DisposeActionWrapper(() => CurrentObject = old);
        }

        /// <summary>
        /// Creates a child scope under this scope with indices associated with the <see cref="ITableMapper"/> specified
        /// </summary>
        /// <param name="owner">The table mapper to associate with the child scope</param>
        /// <returns></returns>
        public ProcessingScope CreateChildScope(ITableMapper owner)
        {
            ProcessingScope childScope;
            if (!_children.TryGetValue(owner, out childScope))
            {
                _children.Add(owner, childScope = new ProcessingScope(this));
            }

            childScope.CurrentObject = null;

            return childScope;
        }


        /// <summary>
        /// Returns the index of this scope relative to the scope specified
        /// </summary>
        public int Index(ProcessingScope parentScope)
        {
            if (parentScope == this || parentScope == null) return _indices[0];

            return _indices[parentScope.Depth + 1];
        }

        /// <summary>
        /// This index of this scope relative to the scope containing an object of the type specified
        /// </summary>
        /// <typeparam name="TObject">The type of the object to be contained in the parent scope</typeparam>
        /// <returns></returns>
        public int Index<TObject>() where TObject : class
        {
            var parentScope = ParentOrSelf<TObject>();
            return parentScope != null ? Index(parentScope) : -1;
        }

        /// <summary>
        /// Finds the closest scope with an item of the type specified
        /// </summary>
        /// <typeparam name="TObject">The type of the object to find in this or a parent scope</typeparam>
        /// <returns></returns>
        public ProcessingScope ParentOrSelf<TObject>() where TObject : class
        {
            var source = CurrentObject as TObject;
            if (source != null)
            {
                return this;
            }

            if (Parent != null)
            {
                return Parent.ParentOrSelf<TObject>();
            }

            return null;
        }


        /// <summary>
        /// Returns the value specified once for the item of the type in this or a parent scope.
        /// This is in particular useful "count distinct" since the number of distinct items in the parent scope can be counted in this way
        /// </summary>
        /// <typeparam name="TObject">The type of the object to find in this or a parent scope</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OncePerScope<TObject>(object value) where TObject : class
        {
            return OncePerScope(ParentOrSelf<TObject>(), value);
        }

        /// <summary>
        /// Returns the value specified once for the item in the scope specified
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OncePerScope(ProcessingScope scope, object value)
        {
            if (scope == this) return value;

            if (scope != null)
            {
                return new OncePerScopeValue(scope, value);
            }

            return null;
        }


        /// <summary>
        /// Maintains a dictionary of the rows "seen" by the item in a parent scope to provide unique values
        /// </summary>
        class OncePerScopeValue : IDeferedValue
        {
            private readonly ProcessingScope _scope;
            private readonly object _value;

            public OncePerScopeValue(ProcessingScope scope, object value)
            {
                _scope = scope;
                _value = value;
            }

            public object GetValue(TableDataBuilder builder, Indexed<Field> field, object[] row)
            {
                var fields = _scope._uniqueChildFields
                    .GetOrAdd(builder, () => new Dictionary<object[], bool[]>(builder.RowMap.Comparer))
                    .GetOrAdd(row, () => new bool[row.Length]);


                var firstTimeInScope = !fields[field.Index];
                fields[field.Index] = true;
                return firstTimeInScope ? _value : null;
            }
        }
    }
}
