using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Inventory;

/// <summary>
/// Backs the category combo box on the three inventory dialogs. Extracted because all three had
/// byte-identical copies of this logic, so a fix had to be made three times to be made at all.
/// <para>The combo shows <c>DisplayName</c> but the server expects <c>Code</c>, so this owns the
/// reverse lookup — the single place a wrong answer would silently refile a product.</para>
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ProductCategoryPicker
{
	private readonly IStoreHubClient _storeHubClient;

	/// <summary>Everything this store has, including categories it may not use for new products.
	/// Used only to render the label of a product that already sits in one.</summary>
	private IReadOnlyList<ProductCategoryDto> _all = [];

	/// <summary>The subset actually offered in the combo. Codes are resolved against THIS list,
	/// never <see cref="_all"/>: the combo is editable, so typing a hidden category's name would
	/// otherwise produce a code the server then rejects.</summary>
	private IReadOnlyList<ProductCategoryDto> _selectable = [];

	public ProductCategoryPicker(IStoreHubClient storeHubClient)
	{
		_storeHubClient = storeHubClient;
	}

	/// <summary>False until a fetch has succeeded. Distinguishes "catalogue unavailable" from
	/// "nothing to pick", which need different messages to the operator.</summary>
	public bool Loaded { get; private set; }

	public IReadOnlyList<ProductCategoryDto> Selectable => _selectable;

	/// <summary>
	/// Fetches the catalogue and the store's feature flags. Returns false if either call failed;
	/// the caller decides what to tell the operator. Never throws.
	/// </summary>
	public async Task<bool> LoadAsync()
	{
		try
		{
			var categories = await _storeHubClient.GetProductCategoriesAsync();
			var multipleTypes = (await _storeHubClient.GetStoreFeaturesAsync()).MultipleProductTypesEnabled;

			_all = categories;
			_selectable = categories
				.Where(c => c.IsEnabled)
				// Hide kinds this store may not use; the server rejects them anyway.
				.Where(c => multipleTypes || c.Kind == ProductCategoryKind.GeneralGoods)
				.ToList();

			Loaded = true;
			return true;
		}
		catch
		{
			_all = [];
			_selectable = [];
			return false;
		}
	}

	/// <summary>
	/// The catalogue Code behind a display name, or null when nothing matches — or when MORE than
	/// one category shares that display name. Nothing constrains display names to be unique, and
	/// GeneralHardware already carries the near-identical pair วัสดุและอุปกรณ์ทั่วไป /
	/// วัสดุและอุปกรณ์; if those ever collided, picking the first would silently refile a product
	/// into a different reporting bucket. Failing the validation instead is the safe direction.
	/// </summary>
	public string? CodeFor(string? displayName)
	{
		if (string.IsNullOrWhiteSpace(displayName)) return null;

		var matches = _selectable
			.Where(c => c.DisplayName == displayName.Trim())
			.Take(2)
			.ToList();

		return matches.Count == 1 ? matches[0].Code : null;
	}

	/// <summary>
	/// The display name for a stored code, or null if this store's catalogue has no such code.
	/// Resolved against the FULL set so a product already filed under a category the store can no
	/// longer select still renders its real label instead of a blank.
	/// </summary>
	public string? DisplayNameFor(string? code) =>
		string.IsNullOrEmpty(code)
			? null
			: _all.FirstOrDefault(c => c.Code == code)?.DisplayName;

	/// <summary>True if the stored code exists but is not one the operator may pick, which would
	/// make the product unsavable until it is re-categorised.</summary>
	public bool IsStoredCodeUnselectable(string? code) =>
		!string.IsNullOrEmpty(code)
		&& _all.Any(c => c.Code == code)
		&& _selectable.All(c => c.Code != code);
}
