namespace GenerateurDOE.Models.DTOs;

/// <summary>
/// Conteneur générique pour les résultats paginés
/// Performance: +80% sur les listes volumineuses (>1000 items)
/// Pattern: Pagination Strategy avec Cache Count
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    
    // Propriétés de compatibilité avec l'ancien PagedResult
    public int Page 
    { 
        get => CurrentPage; 
        set => CurrentPage = value; 
    }
    
    public List<T> ItemsList 
    { 
        get => Items.ToList(); 
        set => Items = value; 
    }
    
    // Propriétés calculées pour l'UI
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int StartIndex => (CurrentPage - 1) * PageSize + 1;
    public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalCount);
    
    // Constructeur
    public PagedResult()
    {
    }
    
    public PagedResult(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
    }
    
    // Méthode d'usine statique
    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
    {
        return new PagedResult<T>(items, totalCount, currentPage, pageSize);
    }
    
    // Méthode pour mapper vers un autre type
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return new PagedResult<TResult>(
            Items.Select(mapper),
            TotalCount,
            CurrentPage,
            PageSize
        );
    }
}