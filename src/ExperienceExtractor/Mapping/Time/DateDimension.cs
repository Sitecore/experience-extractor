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
    public class DateDimension : Dimension
    {        
        private readonly Func<ProcessingScope, DateTime?> _selector;
        private readonly bool _useDateForKey;
        private readonly DateDetailLevel _detailLevel;
        private readonly CultureInfo _cultureInfo;
        private SortOrder _sort;

        private DateFields _mapper;

        public DateDimension(string fieldName, Func<ProcessingScope, DateTime?> selector, string tableName = null, bool inlineFields = false, bool useDateForKey = true, DateDetailLevel detailLevel = DateDetailLevel.Date, CultureInfo cultureInfo = null, SortOrder sort = SortOrder.Unspecified, bool key = false)
            : base(fieldName, tableName ?? fieldName, Enumerable.Empty<IFieldMapper>())
        {            
            _selector = selector;
            _useDateForKey = useDateForKey;
            _detailLevel = detailLevel;
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
            _sort = sort;
            Key = key;

            InlineFields = inlineFields;

            _mapper = new DateFields(this);

            FieldMappers.Add(_mapper);
        }

        protected override TableDataBuilder CreateLookupBuilder()
        {
            return new SequenceTableDataBuilder<DateTime?>(TableName, _mapper);
        }


        class DateFields : FieldMapperBase, ISequenceMapper<DateTime?>
        {
            private readonly DateDimension _owner;

            public DateFields(DateDimension owner)
            {
                _owner = owner;
            }

            public override bool SetValues(ProcessingScope scope, IList<object> target)
            {
                return SetValues(GetKeyFromContext(scope), target);
            }

            public DateTime? GetKeyFromContext(ProcessingScope context)
            {
                return _owner._selector(context);
            }


            public DateTime? Increment(DateTime? value)
            {
                var d = value.Value;

                if (_owner._detailLevel == DateDetailLevel.Year) return d.AddYears(1);
                if (_owner._detailLevel == DateDetailLevel.Quarter) return d.AddMonths(3);
                if (_owner._detailLevel == DateDetailLevel.Month) return d.AddMonths(1);
                if (_owner._detailLevel == DateDetailLevel.Date) return d.AddDays(1);

                throw new ArgumentOutOfRangeException();
            }


            public bool SetValues(DateTime? value, IList<object> row)
            {
                if (!value.HasValue) return false;

                var date = value.Value;

                date = AdjustToDetailLevel(date);
                var index = 0;

                row[index++] = _owner._useDateForKey ? date.Date : (object)DateToInt(date.Date);

                row[index++] = date.Year;
                if (_owner._detailLevel > DateDetailLevel.Year)
                {
                    var quarter = ((date.Month / 4) + 1);
                    var quarterName = "Q" + ((date.Month / 4) + 1);
                    row[index++] = date.Year * 100 + quarter;
                    row[index++] = quarterName + " " + date.Year;
                    row[index++] = quarter;
                    row[index++] = quarterName;

                    if (_owner._detailLevel > DateDetailLevel.Quarter)
                    {
                        row[index++] = date.Year * 100 + date.Month;
                        row[index++] =
                            Capitalize(_owner._cultureInfo.DateTimeFormat.GetAbbreviatedMonthName(date.Month)) +
                            " " + date.Year;
                        row[index++] = date.Month;
                        row[index++] = Capitalize(_owner._cultureInfo.DateTimeFormat.GetMonthName(date.Month));

                        if (_owner._detailLevel > DateDetailLevel.Month)
                        {
                            row[index++] = date.ToString("d", _owner._cultureInfo);
                            row[index++] = date.ToString("D", _owner._cultureInfo);
                            row[index++] = date.DayOfYear;
                            row[index++] = (int)date.DayOfWeek;
                            row[index++] = Capitalize(_owner._cultureInfo.DateTimeFormat.GetDayName(date.DayOfWeek));
                        }
                    }
                }

                return true;
            }

            protected override IEnumerable<Field> CreateFields()
            {                
                var keyName = (_owner.InlineFields ? "" : _owner.TableName + "Id");

                yield return
                    new Field
                    {
                        Name = keyName,
                        FieldType = FieldType.Key,
                        SortOrder = _owner._sort,
                        ValueType = _owner._useDateForKey ? typeof(DateTime) : typeof(int)
                    };

                
                yield return
                    new Field { Name = "Year", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                if (_owner._detailLevel > DateDetailLevel.Year)
                {
                    yield return
                        new Field { Name = "Quarter", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                    yield return
                        new Field
                        {
                            Name = "QuarterLabel",
                            FieldType = FieldType.Label,
                            ValueType = typeof(string),
                            SortBy = "Quarter"
                        };
                    yield return
                        new Field { Name = "QuarterNumber", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                    yield return
                        new Field { Name = "QuarterName", FieldType = FieldType.Label, ValueType = typeof(string) };
                    if (_owner._detailLevel > DateDetailLevel.Quarter)
                    {
                        yield return
                            new Field { Name = "Month", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                        yield return
                            new Field
                            {
                                Name = "MonthLabel",
                                FieldType = FieldType.Label,
                                ValueType = typeof(string),
                                SortBy = "Month"
                            };
                        yield return
                            new Field { Name = "MonthNumber", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                        yield return
                            new Field
                            {
                                Name = "MonthName",
                                FieldType = FieldType.Label,
                                ValueType = typeof(string),
                                SortBy = "MonthNumber"
                            };

                        if (_owner._detailLevel > DateDetailLevel.Month)
                        {
                            yield return
                                new Field
                                {
                                    Name = "ShortDateLabel",
                                    FieldType = FieldType.Label,
                                    ValueType = typeof(string),
                                    SortBy = keyName
                                };
                            yield return
                                new Field
                                {
                                    Name = "LongDateLabel",
                                    FieldType = FieldType.Label,
                                    ValueType = typeof(string),
                                    SortBy = keyName
                                };
                            yield return
                                new Field { Name = "DayOfYear", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                            yield return
                                new Field { Name = "Weekday", FieldType = FieldType.Dimension, ValueType = typeof(int) };
                            yield return
                                new Field
                                {
                                    Name = "WeekdayName",
                                    FieldType = FieldType.Label,
                                    ValueType = typeof(string),
                                    SortBy = "Weekday"
                                };
                        }
                    }
                }
            }

            private DateTime AdjustToDetailLevel(DateTime d)
            {
                switch (_owner._detailLevel)
                {
                    case DateDetailLevel.Month:
                        return new DateTime(d.Year, d.Month, 1);
                    case DateDetailLevel.Quarter:
                        return new DateTime(d.Year, 1 + (d.Month / 4) * 3, 1);
                    case DateDetailLevel.Year:
                        return new DateTime(d.Year, 1, 1);
                }

                return d.Date;
            }

            private int DateToInt(DateTime d)
            {
                return d.Year * 10000 + d.Month * 100 + d.Day;
            }

            private string Capitalize(string s)
            {
                if (s.Length > 0)
                {
                    return s.Substring(0, 1).ToUpper() + s.Substring(1);
                }
                return s;
            }
        }
    }
}