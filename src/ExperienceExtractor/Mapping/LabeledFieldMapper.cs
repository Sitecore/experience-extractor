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

using System.Collections.Generic;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Labels;

namespace ExperienceExtractor.Mapping
{
    public class LabeledFieldMapper : FieldMapperBase
    {
        protected IFieldMapper Key { get; set; }
        protected IEnumerable<KeyValuePair<string, ILabelProvider>> Labels { get; set; }
        public string FriendlyName { get; set; }

        public LabeledFieldMapper(IFieldMapper key, string labelName, ILabelProvider label, string friendlyName = null)
            : this(key, label == null ? new KeyValuePair<string, ILabelProvider>[0] : new[] { new KeyValuePair<string, ILabelProvider>(labelName, label)}, friendlyName)
        {

        }

        public LabeledFieldMapper(IFieldMapper key, IEnumerable<KeyValuePair<string, ILabelProvider>> labels = null, string friendlyName = null)
        {
            Key = key;
            Labels = labels;
            FriendlyName = friendlyName;
        }


        private List<LabelLoader> _labelLoaders;
        protected override IEnumerable<Field> CreateFields()
        {
            foreach (var field in Key.Fields)
            {
                yield return field;
            }

            if (Labels != null)
            {
                foreach (var label in Labels)
                {
                    yield return new Field { FieldType = FieldType.Label, Name = label.Key, ValueType = typeof(string), FriendlyName = FriendlyName};
                }
            }
        }

        public override bool SetValues(ProcessingScope scope, IList<object> target)
        {
            return Key.SetValues(scope, target);
        }

        public override void Initialize(DataProcessor processor)
        {
            Key.Initialize(processor);

            _labelLoaders = new List<LabelLoader>();
            if (Labels != null)
            {
                foreach (var label in Labels)
                {
                    var loader = new LabelLoader(label.Value);
                    loader.LabelProvider.Initialize(processor);
                    _labelLoaders.Add(loader);
                }
            }

            base.Initialize(processor);
        }

        public override void PostProcessRows(IEnumerable<IList<object>> rows)
        {
            Key.PostProcessRows(rows);

            var i = Key.Fields.Count;
            foreach (var loader in _labelLoaders)
            {
                loader.SetLabels(rows, 0, i++);
            }

            base.PostProcessRows(rows);
        }
    }
}
