using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using PWebShop.Admin.Models;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

[Authorize(Roles = $"{ApplicationRoleNames.Administrator},{ApplicationRoleNames.Employee}")]
public partial class Categories : ComponentBase
{
    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private readonly List<CategoryTreeItem> _tree = new();
    private readonly List<CategoryDto> _flatCategories = new();
    private CategoryEditModel? _editorModel;
    private bool _isEditMode;
    private string? _statusMessage;
    private string? _errorMessage;
    private int? _menuOpenForId;
    private int? _draggingCategoryId;

    protected override async Task OnInitializedAsync()
    {
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync(int? selectId = null)
    {
        _statusMessage = null;
        _errorMessage = null;
        _menuOpenForId = null;
        _draggingCategoryId = null;

        var categories = await DbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.ParentId)
            .ThenBy(c => c.SortOrder)
            .ToListAsync();

        var lookup = categories.ToLookup(c => c.ParentId);
        List<CategoryTreeItem> BuildTree(int? parentId) => lookup[parentId]
            .Select(c => new CategoryTreeItem
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                Children = BuildTree(c.Id)
            })
            .OrderBy(c => c.SortOrder)
            .ToList();

        _tree.Clear();
        _tree.AddRange(BuildTree(null));

        _flatCategories.Clear();
        void Flatten(IEnumerable<CategoryTreeItem> nodes, int depth)
        {
            foreach (var node in nodes.OrderBy(n => n.SortOrder))
            {
                _flatCategories.Add(new CategoryDto
                {
                    Id = node.Id,
                    ParentId = node.ParentId,
                    DisplayName = node.DisplayName,
                    SortOrder = node.SortOrder,
                    Depth = depth
                });

                if (node.Children.Count > 0)
                {
                    Flatten(node.Children, depth + 1);
                }
            }
        }

        Flatten(_tree, 0);

        if (selectId.HasValue)
        {
            await SelectCategoryAsync(selectId.Value);
        }
        else if (_editorModel is null && _tree.Count > 0)
        {
            await SelectCategoryAsync(_tree[0].Id);
        }
    }

    private RenderFragment RenderCategoryItem(CategoryTreeItem item, int depth) => builder =>
    {
        var seq = 0;
        builder.OpenElement(seq++, "li");
        builder.AddAttribute(seq++, "class", "category-node mb-1");

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "drop-zone before");
        builder.AddAttribute(seq++, "ondragover", EventCallback.Factory.Create<DragEventArgs>(this, AllowDrop));
        builder.AddAttribute(seq++, "ondrop", EventCallback.Factory.Create<DragEventArgs>(this, () => HandleDropAsync(item.Id, DropPosition.Before)));
        builder.CloseElement();

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", $"category-row {(item.Id == _editorModel?.Id ? "active" : string.Empty)}");
        builder.AddAttribute(seq++, "style", $"padding-left:{depth * 16}px");
        builder.AddAttribute(seq++, "draggable", true);
        builder.AddAttribute(seq++, "ondragstart", EventCallback.Factory.Create<DragEventArgs>(this, () => OnDragStart(item.Id)));
        builder.AddAttribute(seq++, "ondragend", EventCallback.Factory.Create<DragEventArgs>(this, OnDragEnd));
        builder.AddAttribute(seq++, "ondragover", EventCallback.Factory.Create<DragEventArgs>(this, AllowDrop));
        builder.AddAttribute(seq++, "ondrop", EventCallback.Factory.Create<DragEventArgs>(this, () => HandleDropAsync(item.Id, DropPosition.Child)));

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "d-flex align-items-center flex-grow-1");

        if (item.Children.Count > 0)
        {
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "class", "btn btn-link btn-sm text-secondary me-1");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => ToggleExpand(item.Id)));
            builder.AddContent(seq++, item.IsExpanded ? "▾" : "▸");
            builder.CloseElement();
        }
        else
        {
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "placeholder-toggle me-3");
            builder.CloseElement();
        }

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "flex-grow-1 cursor-pointer");
        builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => SelectCategoryAsync(item.Id)));
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "fw-semibold");
        builder.AddContent(seq++, item.DisplayName);
        builder.CloseElement();
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "text-muted small");
        builder.AddContent(seq++, item.Name);
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "d-flex align-items-center gap-2");
        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", item.IsActive ? "badge bg-success" : "badge bg-secondary");
        builder.AddContent(seq++, item.IsActive ? "Active" : "Inactive");
        builder.CloseElement();

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "position-relative");
        builder.OpenElement(seq++, "button");
        builder.AddAttribute(seq++, "type", "button");
        builder.AddAttribute(seq++, "class", "btn btn-link text-secondary p-1");
        builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => ToggleMenu(item.Id)));
        builder.AddContent(seq++, "⋮");
        builder.CloseElement();

        if (_menuOpenForId == item.Id)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "quick-menu shadow-sm");
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "class", "dropdown-item");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => StartCreate(item.Id)));
            builder.AddContent(seq++, "Add subcategory");
            builder.CloseElement();

            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "class", "dropdown-item");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => StartEdit(item.Id)));
            builder.AddContent(seq++, "Edit details");
            builder.CloseElement();

            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "class", "dropdown-item text-danger");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => ConfirmDeleteAsync(item.Id)));
            builder.AddContent(seq++, "Delete");
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "drop-zone after");
        builder.AddAttribute(seq++, "ondragover", EventCallback.Factory.Create<DragEventArgs>(this, AllowDrop));
        builder.AddAttribute(seq++, "ondrop", EventCallback.Factory.Create<DragEventArgs>(this, () => HandleDropAsync(item.Id, DropPosition.After)));
        builder.CloseElement();

        if (item.IsExpanded && item.Children.Count > 0)
        {
            builder.OpenElement(seq++, "ul");
            builder.AddAttribute(seq++, "class", "list-unstyled mb-0");
            foreach (var child in item.Children.OrderBy(c => c.SortOrder))
            {
                builder.AddContent(seq++, RenderCategoryItem(child, depth + 1));
            }
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private async Task SelectCategoryAsync(int categoryId)
    {
        var entity = await DbContext.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (entity is null)
        {
            _editorModel = null;
            return;
        }

        _menuOpenForId = null;
        _isEditMode = true;
        _editorModel = new CategoryEditModel
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    private void StartCreate(int? parentId)
    {
        _statusMessage = null;
        _errorMessage = null;
        _menuOpenForId = null;
        _isEditMode = false;
        _editorModel = new CategoryEditModel
        {
            ParentId = parentId,
            IsActive = true
        };
    }

    private async Task StartEdit(int categoryId)
    {
        _menuOpenForId = null;
        await SelectCategoryAsync(categoryId);
    }

    private void ResetEditor()
    {
        _editorModel = null;
        _isEditMode = false;
        _menuOpenForId = null;
    }

    private async Task SaveAsync(EditContext _)
    {
        if (_editorModel is null)
        {
            return;
        }

        if (!_isEditMode)
        {
            await CreateCategoryAsync();
        }
        else
        {
            await UpdateCategoryAsync();
        }
    }

    private async Task CreateCategoryAsync()
    {
        if (_editorModel is null)
        {
            return;
        }

        _statusMessage = null;
        _errorMessage = null;

        var siblingSortOrder = await DbContext.Categories
            .Where(c => c.ParentId == _editorModel.ParentId)
            .Select(c => c.SortOrder)
            .DefaultIfEmpty(0)
            .MaxAsync();

        var entity = new Category
        {
            Name = _editorModel.Name,
            DisplayName = _editorModel.DisplayName,
            Description = _editorModel.Description,
            ParentId = _editorModel.ParentId,
            IsActive = _editorModel.IsActive,
            SortOrder = siblingSortOrder + 10
        };

        DbContext.Categories.Add(entity);
        await DbContext.SaveChangesAsync();

        _statusMessage = "Category created successfully.";
        await LoadCategoriesAsync(entity.Id);
    }

    private async Task UpdateCategoryAsync()
    {
        if (_editorModel?.Id is null)
        {
            return;
        }

        _statusMessage = null;
        _errorMessage = null;

        var entity = await DbContext.Categories.FirstOrDefaultAsync(c => c.Id == _editorModel.Id.Value);
        if (entity is null)
        {
            _errorMessage = "Category not found.";
            return;
        }

        if (_editorModel.ParentId == entity.Id)
        {
            _errorMessage = "A category cannot be its own parent.";
            return;
        }

        if (await IsDescendantAsync(_editorModel.ParentId, entity.Id))
        {
            _errorMessage = "Cannot move a category under its own descendant.";
            return;
        }

        var parentChanged = entity.ParentId != _editorModel.ParentId;
        var oldParentId = entity.ParentId;

        entity.Name = _editorModel.Name;
        entity.DisplayName = _editorModel.DisplayName;
        entity.Description = _editorModel.Description;
        entity.IsActive = _editorModel.IsActive;
        entity.ParentId = _editorModel.ParentId;

        if (parentChanged)
        {
            var siblingSortOrder = await DbContext.Categories
                .Where(c => c.ParentId == entity.ParentId && c.Id != entity.Id)
                .Select(c => c.SortOrder)
                .DefaultIfEmpty(0)
                .MaxAsync();

            entity.SortOrder = siblingSortOrder + 10;

            var oldSiblings = await DbContext.Categories
                .Where(c => c.ParentId == oldParentId && c.Id != entity.Id)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            for (var i = 0; i < oldSiblings.Count; i++)
            {
                oldSiblings[i].SortOrder = i * 10;
            }
        }

        await DbContext.SaveChangesAsync();

        _statusMessage = "Category updated.";
        await LoadCategoriesAsync(entity.Id);
    }

    private async Task<bool> IsDescendantAsync(int? newParentId, int categoryId)
    {
        if (!newParentId.HasValue)
        {
            return false;
        }

        var all = await DbContext.Categories.AsNoTracking().ToListAsync();
        var lookup = all.ToLookup(c => c.ParentId);
        var stack = new Stack<int>(lookup[categoryId].Select(c => c.Id));

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == newParentId)
            {
                return true;
            }

            foreach (var child in lookup[current])
            {
                stack.Push(child.Id);
            }
        }

        return false;
    }

    private async Task ConfirmDeleteAsync(int categoryId)
    {
        _menuOpenForId = null;
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this category?");
        if (!confirmed)
        {
            return;
        }

        await DeleteCategoryAsync(categoryId);
    }

    private async Task DeleteCategoryAsync(int categoryId)
    {
        _statusMessage = null;
        _errorMessage = null;

        var hasChildren = await DbContext.Categories.AnyAsync(c => c.ParentId == categoryId);
        if (hasChildren)
        {
            _errorMessage = "Cannot delete a category that has children.";
            return;
        }

        var hasProducts = await DbContext.Products.AnyAsync(p => p.CategoryId == categoryId);
        if (hasProducts)
        {
            _errorMessage = "Cannot delete a category that has products.";
            return;
        }

        var entity = await DbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (entity is null)
        {
            _errorMessage = "Category not found.";
            return;
        }

        DbContext.Categories.Remove(entity);
        await DbContext.SaveChangesAsync();

        _statusMessage = "Category deleted.";
        ResetEditor();
        await LoadCategoriesAsync();
    }

    private void ToggleExpand(int categoryId)
    {
        var node = FindNode(_tree, categoryId);
        if (node is null)
        {
            return;
        }

        node.IsExpanded = !node.IsExpanded;
    }

    private CategoryTreeItem? FindNode(IEnumerable<CategoryTreeItem> items, int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item;
            }

            var child = FindNode(item.Children, id);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private void ToggleMenu(int categoryId)
    {
        _menuOpenForId = _menuOpenForId == categoryId ? null : categoryId;
    }

    private static string GetIndentedDisplay(CategoryDto dto)
    {
        if (dto.Depth <= 0)
        {
            return dto.DisplayName;
        }

        var prefix = new string('—', dto.Depth);
        return $"{prefix} {dto.DisplayName}";
    }

    private void OnDragStart(int categoryId)
    {
        _draggingCategoryId = categoryId;
    }

    private void OnDragEnd(DragEventArgs _)
    {
        _draggingCategoryId = null;
    }

    private void AllowDrop(DragEventArgs args)
    {
        args.PreventDefault();
    }

    private async Task HandleDropAsync(int targetId, DropPosition position)
    {
        if (!_draggingCategoryId.HasValue || _draggingCategoryId == targetId)
        {
            return;
        }

        _statusMessage = null;
        _errorMessage = null;

        var all = await DbContext.Categories.ToListAsync();
        var dragging = all.FirstOrDefault(c => c.Id == _draggingCategoryId);
        var target = all.FirstOrDefault(c => c.Id == targetId);

        if (dragging is null || target is null)
        {
            return;
        }

        if (IsDescendant(target.Id, dragging.Id, all))
        {
            _errorMessage = "Cannot move a category under its own descendant.";
            return;
        }

        var oldParentId = dragging.ParentId;
        int? newParentId = position == DropPosition.Child ? target.Id : target.ParentId;

        var newSiblings = all
            .Where(c => c.ParentId == newParentId && c.Id != dragging.Id)
            .OrderBy(c => c.SortOrder)
            .ToList();

        var targetIndex = newSiblings.FindIndex(c => c.Id == target.Id);
        if (targetIndex < 0)
        {
            targetIndex = newSiblings.Count;
        }

        var insertIndex = position switch
        {
            DropPosition.Before => targetIndex,
            DropPosition.After => targetIndex + 1,
            DropPosition.Child => newSiblings.Count,
            _ => newSiblings.Count
        };

        newSiblings.Insert(insertIndex, dragging);
        dragging.ParentId = newParentId;

        for (var i = 0; i < newSiblings.Count; i++)
        {
            newSiblings[i].SortOrder = i * 10;
        }

        if (oldParentId != newParentId)
        {
            var oldSiblings = all
                .Where(c => c.ParentId == oldParentId && c.Id != dragging.Id)
                .OrderBy(c => c.SortOrder)
                .ToList();

            for (var i = 0; i < oldSiblings.Count; i++)
            {
                oldSiblings[i].SortOrder = i * 10;
            }
        }

        await DbContext.SaveChangesAsync();
        _statusMessage = "Category position updated.";
        await LoadCategoriesAsync(dragging.Id);
    }

    private static bool IsDescendant(int targetId, int sourceId, List<Category> all)
    {
        var lookup = all.ToLookup(c => c.ParentId);
        var stack = new Stack<int>(lookup[sourceId].Select(c => c.Id));

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == targetId)
            {
                return true;
            }

            foreach (var child in lookup[current])
            {
                stack.Push(child.Id);
            }
        }

        return false;
    }

    private class CategoryDto
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int Depth { get; set; }
    }
}
