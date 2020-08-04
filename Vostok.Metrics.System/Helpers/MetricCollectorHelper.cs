using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vostok.Metrics.Models;

// ReSharper disable PossibleMultipleEnumeration

namespace Vostok.Metrics.System.Helpers
{
    internal static class MetricCollectorHelper
    {
        public static IEnumerable<MetricDataPoint> ModelToMetricDataPoints(object obj)
        {
            return ModelToMetricDataPoints(obj, new List<(string key, string value)>());
        }

        private static bool IsEnumerableType(Type type)
        {
            return type.GetInterface(nameof(IEnumerable)) != null;
        }

        private static bool IsDictionaryType(Type type)
        {
            return type.GetInterface(nameof(IDictionary)) != null;
        }

        private static bool TryConvertToDouble(object obj, out double value)
        {
            try
            {
                value = Convert.ToDouble(obj);
                return true;
            }
            catch
            {
                value = double.NaN;
                return false;
            }
        }

        private static IEnumerable<MetricDataPoint> ModelToMetricDataPoints(
            object obj,
            IEnumerable<(string key, string value)> tags)
        {
            foreach (var property in obj.GetType().GetProperties())
            {
                var propertyValue = property.GetValue(obj);

                if (TryConvertToDouble(propertyValue, out var value))
                    yield return new MetricDataPoint(value, tags.Append((WellKnownTagKeys.Name, property.Name)).ToArray());
                else
                {
                    if (IsDictionaryType(property.PropertyType))
                    {
                        foreach (var dataPoint in DictionaryToMetricDataPoints(propertyValue as IDictionary, property.Name, tags))
                            yield return dataPoint;
                    }
                    else if (IsEnumerableType(property.PropertyType))
                    {
                        foreach (var dataPoint in CollectionToMetricDataPoints(propertyValue as IEnumerable, property.Name, tags))
                            yield return dataPoint;
                    }
                    else
                    {
                        foreach (var dataPoint in ModelToMetricDataPoints(propertyValue, tags))
                            yield return dataPoint;
                    }
                }
            }
        }

        private static IEnumerable<MetricDataPoint> DictionaryToMetricDataPoints(
            IDictionary dict,
            string dictPropName,
            IEnumerable<(string key, string value)> tags)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (TryConvertToDouble(entry.Value, out var value))
                {
                    var newTags =
                        tags.Append((WellKnownTagKeys.Name, dictPropName))
                            .Append(($"{dictPropName}Key", entry.Key.ToString()));

                    yield return new MetricDataPoint(value, newTags.ToArray());
                }
                else
                {
                    var newTags = tags.Append(($"{dictPropName}Key", entry.Key.ToString()));

                    foreach (var dataPoint in ModelToMetricDataPoints(entry.Value, newTags))
                        yield return dataPoint;
                }
            }
        }

        private static IEnumerable<MetricDataPoint> CollectionToMetricDataPoints(
            IEnumerable collection,
            string collectionPropName,
            IEnumerable<(string key, string value)> tags)
        {
            var index = 0;
            var newTags = tags.Append((WellKnownTagKeys.Name, collectionPropName));

            foreach (var obj in collection)
            {
                var tagsWithIndex = newTags.Append(($"{collectionPropName}Index", index++.ToString()));

                if (TryConvertToDouble(obj, out var value))
                    yield return new MetricDataPoint(value, tagsWithIndex.ToArray());
            }
        }
    }
}