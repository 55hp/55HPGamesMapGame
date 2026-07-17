using System.Collections.Generic;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.Mobile.Core.Architecture
{
    /// <summary>
    /// Smista i config di gameplay raccolti in un ConfigCatalog, per accesso tipizzato dai
    /// consumer senza wiring per-componente. Distinto da IConfigService (GameConfig app-level
    /// via Addressables): questo copre i ScriptableObject di gameplay che implementano
    /// IConfigAsset.
    /// </summary>
    public interface IConfigCatalogService
    {
        /// <summary>L'unico config di tipo T, o null (con errore loggato) se assente.</summary>
        T Get<T>() where T : ScriptableObject, IConfigAsset;

        /// <summary>Tutti i config di tipo T. Lista vuota se nessuno.</summary>
        IReadOnlyList<T> GetAll<T>() where T : ScriptableObject, IConfigAsset;
    }

    public sealed class ConfigCatalogService : IConfigCatalogService
    {
        private readonly ConfigCatalog _catalog;

        public ConfigCatalogService(ConfigCatalog catalog)
        {
            _catalog = catalog;
            if (_catalog == null)
                Debug.LogError("[ConfigCatalogService] Catalog null in costruzione. Assegna un ConfigCatalog al ConfigCatalogInstaller.");
        }

        public T Get<T>() where T : ScriptableObject, IConfigAsset
            => _catalog != null ? _catalog.Get<T>() : null;

        public IReadOnlyList<T> GetAll<T>() where T : ScriptableObject, IConfigAsset
            => _catalog != null ? _catalog.GetAll<T>() : new List<T>();
    }
}
