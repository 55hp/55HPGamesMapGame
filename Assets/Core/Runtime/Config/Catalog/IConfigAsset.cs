namespace hp55games.Mobile.Core.Config
{
    /// <summary>
    /// Marker per ogni ScriptableObject di configurazione di gioco che deve poter essere
    /// raccolto e smistato dal ConfigCatalog. Implementarlo su un config lo rende
    /// individuabile dallo scan editor (vedi ConfigCatalogEditor) e risolvibile a runtime
    /// via IConfigCatalogService.Get&lt;T&gt;().
    ///
    /// NON ha membri: serve solo come vincolo di tipo. Sistema distinto da IConfigService
    /// (hp55games.Mobile.Core.Architecture), che carica la GameConfig app-level singola via
    /// Addressables — questo invece aggrega piu' config di gameplay per accesso tipizzato.
    /// </summary>
    public interface IConfigAsset
    {
    }
}
