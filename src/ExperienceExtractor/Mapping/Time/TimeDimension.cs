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
using System.Globalization;
using System.Linq;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DateTime;

namespace ExperienceExtractor.Mapping.Time
{
    public class TimeDimension : Dimension
    {
        private readonly Func<ProcessingScope, DateTime?> _selector;
        private readonly bool _useTimeForKey;
        private readonly TimeDetailLevel _detailLevel;
        private readonly CultureInfo _cultureInfo;

        private readonly TimeFields _mapper;

        public TimeDimension(string fieldName, Func<ProcessingScope, DateTime?> selector, string tableName = null, bool inlineFields = false, bool useTimeForKey = false, TimeDetailLevel detailLevel = TimeDetailLevel.Minute, CultureInfo cultureInfo = null, bool key = false)
            : base(fieldName, tableName ?? fieldName, Enumerable.Empty<IFieldMapper>())
        {
            _selector = selector;
            _useTimeForKey = useTimeForKey;
            _detailLevel = detailLevel;
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;            
            _mapper = new TimeFields(this);
            Key = key;

            InlineFields = inlineFields;
            FieldMappers.Add(_mapper);
        }

        protected override TableDataBuilder CreateLookupBuilder()
        {
            return new SequenceTableDataBuilder<TimeSpan?>(TableName, _mapper)
            {
                Min = TimeSpan.Zero,
                Max = new TimeSpan(0, 24, 0, 0)
            };
        }

        class TimeFields : FieldMapperBase, ISequenceMapper<TimeSpan?>
        {
            private readonly TimeDimension _owner;

            public TimeFields(TimeDimension owner)
            {
                _owner = owner;
            }

            public TimeSpan? GetKeyFromContext(ProcessingScope context)
            {
                var d = _owner._selector(context);
                return d.HasValue ? d.Value.TimeOfDay : (TimeSpan?)null;
            }

            public bool SetValues(TimeSpan? value, IList<object> row)
            {
                if (!value.HasValue) return false;

                var time = AdjustToDetailLevel(value.Value);

                var index = 0;

                row[index++] = _owner._useTimeForKey ? time : (object)TimeToInt(time);                

                row[index++] = time.Hours;
                row[index++] = new DateTime().Add(time).ToString("t", _owner._cultureInfo);
                if (_owner._detailLevel > TimeDetailLevel.Hour)
                {
                    row[index++] = time.Minutes;
                    if (_owner._detailLevel > TimeDetailLevel.Minute)
                    {
                        row[index++] = time.Seconds;
                    }
                }

                return true;
            }

            public TimeSpan? Increment(TimeSpan? value)
            {
                var t = value.Value;

                if (_owner._detailLevel == TimeDetailLevel.Hour) return t.Add(TimeSpan.FromHours(1));
                else if (_owner._detailLevel == TimeDetailLevel.Quarter) return t.Add(TimeSpan.FromMinutes(15));
                else if (_owner._detailLevel == TimeDetailLevel.Minute) return t.Add(TimeSpan.FromSeconds(1));

                throw new ArgumentOutOfRangeException();
            }

            protected override IEnumerable<Field> CreateFields()
            {
                var keyName = (_owner.InlineFields ? "" : _owner.TableName + "Id");

                yield return
                    new Field
                    {
                        Name = keyName,
                        FieldType = FieldType.Key,
                        Hide=true,
                        //SortOrder =  ? SortOrder.Ascending : SortOrder.Unspecified,
                        ValueType = _owner._useTimeForKey ? typeof(TimeSpan) : typeof(int)
                    };

                
                yield return
                    new Field
                    {
                        Name = "Hour",
                        FieldType = FieldType.Dimension,
                        ValueType = typeof(int),
                        //    ExtendedProperties = new Dictionary<string, object> { { "SortBy", keyName } }
                    };
                yield return
                    new Field
                    {
                        Name = "Label",
                        FieldType = FieldType.Label,
                        ValueType = typeof(string),
                        SortBy = keyName,
                        FriendlyName = "Time of day"
                    };
                if (_owner._detailLevel > TimeDetailLevel.Hour)
                {
                    yield return
                        new Field { Name = "Minute", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                    if (_owner._detailLevel > TimeDetailLevel.Minute)
                    {
                        yield return
                            new Field
                            {
                                Name = "Second",
                                FieldType = FieldType.Dimension,
                                ValueType = typeof(int)
                            };
                    }
                }
            }

            private TimeSpan AdjustToDetailLevel(TimeSpan t)
            {
                switch (_owner._detailLevel)
                {
                    case TimeDetailLevel.Hour:
                        return new TimeSpan(t.Hours, 0, 0);
                    case TimeDetailLevel.Quarter:
                        return new TimeSpan(t.Hours, 15 * (t.Minutes / 15), 0);
                }

                return new TimeSpan(t.Hours, t.Minutes, 0); ;
            }

            private int TimeToInt(TimeSpan t)
            {
                return t.Hours * 10000 + t.Minutes * 100 + t.Seconds;
            }

            public override bool SetValues(ProcessingScope scope, IList<object> target)
            {
                return SetValues(GetKeyFromContext(scope), target);
            }
        }
    }
}