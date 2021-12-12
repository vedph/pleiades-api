using System;
using System.Collections.Generic;

namespace Pleiades.Core
{
    /// <summary>
    /// A set of lookup data, incrementally populated while reading places.
    /// Each lookup entry has a numeric ID (1-N) and a corresponding full name,
    /// plus eventually a short name. The full name can be anything, even if
    /// in most cases it's a URI. When not a URI, it may have a conventional
    /// prefix to represent a subset: e.g. review states, represented by full
    /// names in the original dataset, are encoded with numeric IDs. In this
    /// case, their full name is built by prefixing a state set prefix like
    /// <c>@state:</c> to the full name.
    /// <para>This class is not safe for multithreading, as it's built for a
    /// linear import process.</para>
    /// </summary>
    public sealed class LookupEntrySet
    {
        public const string STATE_GROUP = "state";
        public const string NAMETYPE_GROUP = "name-type";
        public const string NAMETRAC_GROUP = "name-trac";
        public const string NAMETRCP_GROUP = "name-trcp";
        public const string CONNCERT_GROUP = "conn-cert";
        public const string REFTYPE_GROUP = "ref-type";
        public const string REFCITTYPE_GROUP = "ref-cit-type";

        private readonly Dictionary<Tuple<string, string>, LookupEntry> _lookups;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupEntrySet"/> class.
        /// </summary>
        public LookupEntrySet()
        {
            _lookups = new Dictionary<Tuple<string, string>, LookupEntry>();
        }

        /// <summary>
        /// Gets the lookup data entries.
        /// </summary>
        /// <returns>Prefixes</returns>
        public IEnumerable<LookupEntry> GetLookups() => _lookups.Values;

        /// <summary>
        /// Clears this set.
        /// </summary>
        public void Clear() => _lookups.Clear();

        private static Tuple<string, string> GetKey(
            string fullName, string group) =>
            Tuple.Create(group, fullName.ToLowerInvariant());

        /// <summary>
        /// Gets the identifier for the specified full name in this set.
        /// If <paramref name="fullName"/> does not exist in this set, it will
        /// be added and its ID will be returned.
        /// </summary>
        /// <param name="fullName">The name. Nothing is done and 0 is always
        /// returned when null or empty.</param>
        /// <param name="shortName">The optional short name.</param>
        /// <param name="group">The optional group.</param>
        /// <returns>ID.</returns>
        public int GetId(string fullName, string shortName = null,
            string group = null)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;

            var key = GetKey(fullName, group);
            if (!_lookups.ContainsKey(key))
            {
                _lookups[key] = new LookupEntry
                {
                    Id = _lookups.Count + 1,
                    Group = group,
                    FullName = fullName,
                    ShortName = shortName
                };
            }
            return _lookups[key].Id;
        }
    }
}
