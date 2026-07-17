using System.Collections.Generic;
using UnityEngine;

namespace hp55games.Mobile.Core.Config
{
    /// <summary>
    /// Contenitore aggregatore di tutti i config di gameplay (IConfigAsset) del progetto.
    /// Popolato in editor dal tasto di scan (ConfigCatalogEditor), che raccoglie ogni
    /// IConfigAsset trovato dentro cartelle "Content" e sottocartelle. A runtime e' di sola
    /// lettura: l'installer lo registra e i consumer lo interrogano via IConfigCatalogService.
    ///
    /// Get&lt;T&gt;() assume un solo asset per tipo (config singleton: MapGenerationConfig,
    /// SurvivalConfig, ...). Per i tipi multi-istanza (es. LevelConfig, uno per missione)
    /// usare GetAll&lt;T&gt;(): Get&lt;T&gt;() logga un warning e restituisce il primo se ne
    /// trova piu' di uno.
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Config Catalog", fileName = "ConfigCatalog")]
    public sealed class ConfigCatalog : ScriptableObject
    {
        [Tooltip("Popolato dal tasto 'Scansiona cartelle Content'. Non modificare a mano se non per rimozioni puntuali.")]
        [SerializeField] private List<ScriptableObject> _configs = new();

        /// <summary>
        /// Restituisce l'unico config di tipo T. Logga un errore e restituisce null se
        /// nessuno e' presente; logga un warning e restituisce il primo se ce n'e' piu' di
        /// uno (caso da evitare per i tipi singleton — vedi GetAll per i multi-istanza).
        /// </summary>
        public T Get<T>() where T : ScriptableObject, IConfigAsset
        {
            T found = null;
            int count = 0;

            foreach (var config in _configs)
            {
                if (config is T match)
                {
                    if (found == null) found = match;
                    count++;
                }
            }

            if (found == null)
                Debug.LogError($"[ConfigCatalog] Nessun config di tipo {typeof(T).Name} nel catalogo. Esegui lo scan e verifica che l'asset sia dentro una cartella Content.", this);
            else if (count > 1)
                Debug.LogWarning($"[ConfigCatalog] {count} config di tipo {typeof(T).Name} nel catalogo: Get<{typeof(T).Name}>() restituisce il primo. Usa GetAll se il tipo e' multi-istanza.", this);

            return found;
        }

        /// <summary>
        /// Tutti i config di tipo T. Lista vuota (mai null) se nessuno e' presente.
        /// </summary>
        public IReadOnlyList<T> GetAll<T>() where T : ScriptableObject, IConfigAsset
        {
            var result = new List<T>();
            foreach (var config in _configs)
            {
                if (config is T match)
                    result.Add(match);
            }
            return result;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Rimpiazza il contenuto del catalogo. Solo per lo scan editor (ConfigCatalogEditor).
        /// </summary>
        public void EditorSetConfigs(List<ScriptableObject> configs)
        {
            _configs = configs;
        }
#endif
    }
}
