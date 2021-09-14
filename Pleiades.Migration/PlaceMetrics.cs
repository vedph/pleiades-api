using Pleiades.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pleiades.Migration
{
    public sealed class PlaceMetrics
    {
        private readonly Dictionary<string, StringMetrics> _data;

        public IReadOnlyDictionary<string, StringMetrics> Data => _data;

        public PlaceMetrics()
        {
            _data = new Dictionary<string, StringMetrics>();
        }

        public void Reset() => this._data.Clear();

        private void UpdateMetricsFromString(string key, string value)
        {
            if (!_data.ContainsKey(key)) _data[key] = new StringMetrics();
            _data[key].UpdateFrom(value);
        }

        private void UpdateMetricsFromObject(object target, string prefix)
        {
            Type t = target.GetType();
            foreach (var prop in t.GetProperties(
                BindingFlags.FlattenHierarchy |
                BindingFlags.Instance |
                BindingFlags.Public))
            {
                if (prop.PropertyType == typeof(string))
                {
                    string key = prefix + prop.Name;
                    UpdateMetricsFromString(key,
                        prop.GetValue(target, null) as string);
                }
            }
        }

        private void UpdateAuthorMetrics(IHasAuthors target)
        {
            // creators
            if (target.Creators?.Count > 0)
            {
                foreach (var creator in target.Creators)
                    UpdateMetricsFromObject(creator, "Author.");
            }

            // contributors
            if (target.Contributors?.Count > 0)
            {
                foreach (var contributor in target.Contributors)
                    UpdateMetricsFromObject(contributor, "Author.");
            }
        }

        private void UpdateSourceMetrics(IHasSources target)
        {
            // attestations
            if (target.Attestations?.Count > 0)
            {
                foreach (var attestation in target.Attestations)
                    UpdateMetricsFromObject(attestation, "Attestation.");
            }

            // references
            if (target.References?.Count > 0)
            {
                foreach (var reference in target.References)
                    UpdateMetricsFromObject(reference, "Reference.");
            }
        }

        public void Update(Place place)
        {
            if (place == null) return;

            UpdateMetricsFromObject(place, "Place.");

            // features
            if (place.Features?.Count > 0)
            {
                foreach (var feature in place.Features)
                    UpdateMetricsFromObject(feature, "PlaceFeature.");
            }

            // creators, contributors
            UpdateAuthorMetrics(place);

            // locations
            if (place.Locations?.Count > 0)
            {
                foreach (var location in place.Locations)
                {
                    UpdateMetricsFromObject(location, "Location.");

                    // creators, contributors
                    UpdateAuthorMetrics(location);

                    // attestations, references
                    UpdateSourceMetrics(location);
                }
            }

            // connections
            if (place.Connections?.Count > 0)
            {
                foreach (var connection in place.Connections)
                {
                    UpdateMetricsFromObject(connection, "Connection.");

                    // creators, contributors
                    UpdateAuthorMetrics(connection);

                    // attestations, references
                    UpdateSourceMetrics(connection);
                }
            }

            // attestations, references
            UpdateSourceMetrics(place);

            // names
            if (place.Names?.Count > 0)
            {
                foreach (var name in place.Names)
                {
                    UpdateMetricsFromObject(name, "Name.");

                    // creators, contributors
                    UpdateAuthorMetrics(name);

                    // attestations, references
                    UpdateSourceMetrics(name);
                }
            }

            // metadata
            if (place.Metadata?.Count > 0)
            {
                foreach (var metadata in place.Metadata)
                    UpdateMetricsFromObject(metadata, "Metadata.");
            }

            // targetUris
            foreach (string s in place.TargetUris)
                UpdateMetricsFromString("Place.TargetUris", s);
        }
    }

    /// <summary>
    /// Metrics related to strings.
    /// </summary>
    public class StringMetrics
    {
        /// <summary>
        /// Gets or sets the maximum length.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the minimum length.
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the string allows null
        /// values.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringMetrics"/> class.
        /// </summary>
        public StringMetrics()
        {
            MaxLength = -1;
            MinLength = -1;
        }

        /// <summary>
        /// Updates these metrics from the specified string.
        /// </summary>
        /// <param name="s">The string.</param>
        public void UpdateFrom(string s)
        {
            if (s == null) IsNullable = true;
            else
            {
                if (s.Length > MaxLength) MaxLength = s.Length;
                if (s.Length < MinLength || MinLength < 0) MinLength = s.Length;
            }
        }
    }
}
