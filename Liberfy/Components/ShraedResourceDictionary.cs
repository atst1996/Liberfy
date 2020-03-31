﻿// Source code by: https://www.wpftutorial.net/MergedDictionaryPerformance.html

using System;
using System.Collections.Generic;
using System.Windows;

namespace Liberfy
{
    /// <summary>
    /// The shared resource dictionary is a specialized resource dictionary
    /// that loads it content only once. If a second instance with the same source
    /// is created, it only merges the resources from the cache.
    /// </summary>
    public class SharedResourceDictionary : ResourceDictionary
    {
        /// <summary>
        /// Internal cache of loaded dictionaries 
        /// </summary>
        public static IDictionary<Uri, ResourceDictionary> _sharedDictionaries = new Dictionary<Uri, ResourceDictionary>();

        /// <summary>
        /// Local member of the source uri
        /// </summary>
        private Uri _sourceUri;

        /// <summary>
        /// Gets or sets the uniform resource identifier (URI) to load resources from.
        /// </summary>
        public new Uri Source
        {
            get => this._sourceUri;
            set
            {
                this._sourceUri = value;

                if (_sharedDictionaries.TryGetValue(value, out var resources))
                {
                    // If the dictionary is already loaded, get it from the cache
                    this.MergedDictionaries.Add(resources);
                }
                else
                {
                    // If the dictionary is not yet loaded, load it by setting
                    // the source of the base class
                    base.Source = value;

                    // add it to the cache
                    _sharedDictionaries.Add(value, this);
                }
            }
        }
    }
}
