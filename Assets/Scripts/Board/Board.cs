using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board
{
	public enum eMatchDirection
	{
		NONE,
		HORIZONTAL,
		VERTICAL,
		ALL
	}

	private int boardSizeX;
	private int boardSizeY;
	public Cell[,] m_cells;
	private Transform m_root;
	private int m_matchMin;
	private Cell[] m_bottomCells;

	public Board(Transform transform, GameSettings gameSettings)
	{
		m_root = transform;
		m_matchMin = gameSettings.MatchesMin;
		this.boardSizeX = gameSettings.BoardSizeX;
		this.boardSizeY = gameSettings.BoardSizeY;
		m_cells = new Cell[boardSizeX, boardSizeY];
		m_bottomCells = new Cell[5];
		CreateBoard();
		CreateBottomRow();
	}

	private void CreateBoard()
	{
		Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
		GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				GameObject go = GameObject.Instantiate(prefabBG);
				go.transform.position = origin + new Vector3(x, y, 0f);
				go.transform.SetParent(m_root);

				Cell cell = go.GetComponent<Cell>();
				cell.Setup(x, y);
				m_cells[x, y] = cell;
			}
		}

		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
				if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
				if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
				if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
			}
		}
	}

	private void CreateBottomRow()
	{
		const int bottomRowSize = 5;
		m_bottomCells = new Cell[bottomRowSize];

		float offsetX = (boardSizeX - bottomRowSize) * 0.5f;
		Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f + offsetX, -boardSizeY * 0.5f - 1f, 0f);
		GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);

		for (int x = 0; x < bottomRowSize; x++)
		{
			GameObject go = GameObject.Instantiate(prefabBG);
			go.transform.position = origin + new Vector3(x, 0f, 0f);
			go.transform.SetParent(m_root);

			Cell cell = go.GetComponent<Cell>();
			cell.Setup(x, -1);
			m_bottomCells[x] = cell;
		}

		Debug.Log($"Bottom row created with {bottomRowSize} cells.");
	}

	internal void Fill()
	{
		int totalCells = boardSizeX * boardSizeY;

		if (totalCells % 3 != 0)
		{
			Debug.LogError($"Total cells ({totalCells}) must be divisible by 3 to ensure winnable game!");
			return;
		}

		List<NormalItem.eNormalType> allTypes = new List<NormalItem.eNormalType>
		{
			NormalItem.eNormalType.TYPE_ONE,
			NormalItem.eNormalType.TYPE_TWO,
			NormalItem.eNormalType.TYPE_THREE,
			NormalItem.eNormalType.TYPE_FOUR,
			NormalItem.eNormalType.TYPE_FIVE,
			NormalItem.eNormalType.TYPE_SIX,
			NormalItem.eNormalType.TYPE_SEVEN
		};

		int minOccurrencesPerType = 3;
		int totalTypes = allTypes.Count;
		int minTotalItems = minOccurrencesPerType * totalTypes;

		if (totalCells < minTotalItems)
		{
			Debug.LogError($"Total cells ({totalCells}) must be at least {minTotalItems} to accommodate all types with at least 3 occurrences!");
			return;
		}

		int baseOccurrences = totalCells / totalTypes;
		int remainingItems = totalCells % totalTypes;

		baseOccurrences = (baseOccurrences / 3) * 3;
		if (baseOccurrences < 3)
		{
			baseOccurrences = 3;
		}

		int totalAssigned = baseOccurrences * totalTypes;
		remainingItems = totalCells - totalAssigned;

		List<int> occurrences = new List<int>(new int[totalTypes]);
		for (int i = 0; i < totalTypes; i++)
		{
			occurrences[i] = baseOccurrences;
		}

		while (remainingItems > 0)
		{
			for (int i = 0; i < totalTypes && remainingItems >= 3; i++)
			{
				occurrences[i] += 3;
				remainingItems -= 3;
			}
		}

		int totalItemsAssigned = occurrences.Sum();
		if (totalItemsAssigned != totalCells)
		{
			Debug.LogError($"Total items assigned ({totalItemsAssigned}) does not match total cells ({totalCells})!");
			return;
		}

		List<NormalItem.eNormalType> itemsToPlace = new List<NormalItem.eNormalType>();
		for (int i = 0; i < totalTypes; i++)
		{
			for (int j = 0; j < occurrences[i]; j++)
			{
				itemsToPlace.Add(allTypes[i]);
			}
		}

		for (int i = itemsToPlace.Count - 1; i > 0; i--)
		{
			int j = UnityEngine.Random.Range(0, i + 1);
			var temp = itemsToPlace[i];
			itemsToPlace[i] = itemsToPlace[j];
			itemsToPlace[j] = temp;
		}

		int itemIndex = 0;
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];
				if (!cell.IsEmpty) continue;

				NormalItem item = new NormalItem();
				item.SetType(itemsToPlace[itemIndex]);
				item.SetView();
				item.SetViewRoot(m_root);

				// Comment: Thêm lệnh để lưu vị trí ban đầu của vật phẩm trên bảng chính
				item.SetInitialPosition(x, y);

				cell.Assign(item);
				cell.ApplyItemPosition(false);

				itemIndex++;
			}
		}

		Debug.Log("Item distribution:");
		foreach (var type in allTypes)
		{
			int count = itemsToPlace.Count(t => t == type);
			Debug.Log($"{type}: {count}");
		}
	}

	internal void Shuffle()
	{
		List<Item> list = new List<Item>();
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				list.Add(m_cells[x, y].Item);
				m_cells[x, y].Free();
			}
		}

		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				int rnd = UnityEngine.Random.Range(0, list.Count);
				m_cells[x, y].Assign(list[rnd]);
				m_cells[x, y].ApplyItemMoveToPosition();

				list.RemoveAt(rnd);
			}
		}
	}

	internal void FillGapsWithNewItems()
	{
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];
				if (!cell.IsEmpty) continue;

				NormalItem item = new NormalItem();
				item.SetType(Utils.GetRandomNormalType());
				item.SetView();
				item.SetViewRoot(m_root);

				// Comment: Thêm lệnh để lưu vị trí ban đầu của vật phẩm mới được tạo khi điền vào ô trống
				item.SetInitialPosition(x, y);

				cell.Assign(item);
				cell.ApplyItemPosition(true);
			}
		}
	}

	internal void ExplodeAllItems()
	{
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];
				cell.ExplodeItem();
			}
		}
	}

	public void Swap(Cell cell1, Cell cell2, Action callback)
	{
		Item item = cell1.Item;
		cell1.Free();
		Item item2 = cell2.Item;
		cell1.Assign(item2);
		cell2.Free();
		cell2.Assign(item);

		item.View.DOMove(cell2.transform.position, 0.3f);
		item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
	}

	public List<Cell> GetHorizontalMatches(Cell cell)
	{
		List<Cell> list = new List<Cell>();
		list.Add(cell);

		Cell newcell = cell;
		while (true)
		{
			Cell neib = newcell.NeighbourRight;
			if (neib == null) break;

			if (neib.IsSameType(cell))
			{
				list.Add(neib);
				newcell = neib;
			}
			else break;
		}

		newcell = cell;
		while (true)
		{
			Cell neib = newcell.NeighbourLeft;
			if (neib == null) break;

			if (neib.IsSameType(cell))
			{
				list.Add(neib);
				newcell = neib;
			}
			else break;
		}

		return list;
	}

	public List<Cell> GetVerticalMatches(Cell cell)
	{
		List<Cell> list = new List<Cell>();
		list.Add(cell);

		Cell newcell = cell;
		while (true)
		{
			Cell neib = newcell.NeighbourUp;
			if (neib == null) break;

			if (neib.IsSameType(cell))
			{
				list.Add(neib);
				newcell = neib;
			}
			else break;
		}

		newcell = cell;
		while (true)
		{
			Cell neib = newcell.NeighbourBottom;
			if (neib == null) break;

			if (neib.IsSameType(cell))
			{
				list.Add(neib);
				newcell = neib;
			}
			else break;
		}

		return list;
	}

	internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
	{
		eMatchDirection dir = GetMatchDirection(matches);

		BonusItem item = new BonusItem();
		switch (dir)
		{
			case eMatchDirection.ALL:
				item.SetType(BonusItem.eBonusType.ALL);
				break;
			case eMatchDirection.HORIZONTAL:
				item.SetType(BonusItem.eBonusType.HORIZONTAL);
				break;
			case eMatchDirection.VERTICAL:
				item.SetType(BonusItem.eBonusType.VERTICAL);
				break;
		}

		if (item != null)
		{
			if (cellToConvert == null)
			{
				int rnd = UnityEngine.Random.Range(0, matches.Count);
				cellToConvert = matches[rnd];
			}

			item.SetView();
			item.SetViewRoot(m_root);

			cellToConvert.Free();
			cellToConvert.Assign(item);
			cellToConvert.ApplyItemPosition(true);
		}
	}

	internal eMatchDirection GetMatchDirection(List<Cell> matches)
	{
		if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

		var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
		if (listH.Count == matches.Count)
		{
			return eMatchDirection.VERTICAL;
		}

		var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
		if (listV.Count == matches.Count)
		{
			return eMatchDirection.HORIZONTAL;
		}

		if (matches.Count > 5)
		{
			return eMatchDirection.ALL;
		}

		return eMatchDirection.NONE;
	}

	internal List<Cell> FindFirstMatch()
	{
		List<Cell> list = new List<Cell>();

		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];

				var listhor = GetHorizontalMatches(cell);
				if (listhor.Count >= m_matchMin)
				{
					list = listhor;
					break;
				}

				var listvert = GetVerticalMatches(cell);
				if (listvert.Count >= m_matchMin)
				{
					list = listvert;
					break;
				}
			}
		}

		return list;
	}

	public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
	{
		var dir = GetMatchDirection(matches);

		var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
		if (bonus == null)
		{
			return matches;
		}

		List<Cell> result = new List<Cell>();
		switch (dir)
		{
			case eMatchDirection.HORIZONTAL:
				foreach (var cell in matches)
				{
					BonusItem item = cell.Item as BonusItem;
					if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
					{
						result.Add(cell);
					}
				}
				break;
			case eMatchDirection.VERTICAL:
				foreach (var cell in matches)
				{
					BonusItem item = cell.Item as BonusItem;
					if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
					{
						result.Add(cell);
					}
				}
				break;
			case eMatchDirection.ALL:
				foreach (var cell in matches)
				{
					BonusItem item = cell.Item as BonusItem;
					if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
					{
						result.Add(cell);
					}
				}
				break;
		}

		return result;
	}

	internal List<Cell> GetPotentialMatches()
	{
		List<Cell> result = new List<Cell>();
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];

				if (cell.NeighbourRight != null)
				{
					result = GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight);
					if (result.Count > 0)
					{
						break;
					}
				}

				if (cell.NeighbourUp != null)
				{
					result = GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp);
					if (result.Count > 0)
					{
						break;
					}
				}

				if (cell.NeighbourBottom != null)
				{
					result = GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom);
					if (result.Count > 0)
					{
						break;
					}
				}

				if (cell.NeighbourLeft != null)
				{
					result = GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft);
					if (result.Count > 0)
					{
						break;
					}
				}

				Cell neib = cell.NeighbourRight;
				if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
				{
					Cell second = LookForTheSecondCellVertical(neib, cell);
					if (second != null)
					{
						result.Add(cell);
						result.Add(neib.NeighbourRight);
						result.Add(second);
						break;
					}
				}

				neib = null;
				neib = cell.NeighbourUp;
				if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
				{
					Cell second = LookForTheSecondCellHorizontal(neib, cell);
					if (second != null)
					{
						result.Add(cell);
						result.Add(neib.NeighbourUp);
						result.Add(second);
						break;
					}
				}
			}

			if (result.Count > 0) break;
		}

		return result;
	}

	private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
	{
		List<Cell> result = new List<Cell>();

		if (neighbour != null && neighbour.IsSameType(cell))
		{
			Cell third = LookForTheThirdCell(target, neighbour);
			if (third != null)
			{
				result.Add(cell);
				result.Add(neighbour);
				result.Add(third);
			}
		}

		return result;
	}

	private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
	{
		if (target == null) return null;
		if (target.IsSameType(main)) return null;

		Cell second = target.NeighbourRight;
		if (second != null && second.IsSameType(main))
		{
			return second;
		}

		second = null;
		second = target.NeighbourLeft;
		if (second != null && second.IsSameType(main))
		{
			return second;
		}

		return null;
	}

	private Cell LookForTheSecondCellVertical(Cell target, Cell main)
	{
		if (target == null) return null;
		if (target.IsSameType(main)) return null;

		Cell second = target.NeighbourUp;
		if (second != null && second.IsSameType(main))
		{
			return second;
		}

		second = null;
		second = target.NeighbourBottom;
		if (second != null && second.IsSameType(main))
		{
			return second;
		}

		return null;
	}

	private Cell LookForTheThirdCell(Cell target, Cell main)
	{
		if (target == null) return null;
		if (target.IsSameType(main)) return null;

		Cell third = CheckThirdCell(target.NeighbourUp, main);
		if (third != null)
		{
			return third;
		}

		third = null;
		third = CheckThirdCell(target.NeighbourRight, main);
		if (third != null)
		{
			return third;
		}

		third = null;
		third = CheckThirdCell(target.NeighbourBottom, main);
		if (third != null)
		{
			return third;
		}

		third = null;
		third = CheckThirdCell(target.NeighbourLeft, main);
		if (third != null)
		{
			return third;
		}

		return null;
	}

	private Cell CheckThirdCell(Cell target, Cell main)
	{
		if (target != null && target != main && target.IsSameType(main))
		{
			return target;
		}

		return null;
	}

	internal void ShiftDownItems()
	{
		for (int x = 0; x < boardSizeX; x++)
		{
			int shifts = 0;
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];
				if (cell.IsEmpty)
				{
					shifts++;
					continue;
				}

				if (shifts == 0) continue;

				Cell holder = m_cells[x, y - shifts];

				Item item = cell.Item;
				cell.Free();

				holder.Assign(item);
				item.View.DOMove(holder.transform.position, 0.3f);
			}
		}
	}

	public void Clear()
	{
		for (int x = 0; x < boardSizeX; x++)
		{
			for (int y = 0; y < boardSizeY; y++)
			{
				Cell cell = m_cells[x, y];
				cell.Clear();
				GameObject.Destroy(cell.gameObject);
				m_cells[x, y] = null;
			}
		}

		for (int x = 0; x < m_bottomCells.Length; x++)
		{
			Cell cell = m_bottomCells[x];
			cell.Clear();
			GameObject.Destroy(cell.gameObject);
			m_bottomCells[x] = null;
		}
	}

	public Cell GetBottomCell(int x)
	{
		if (x >= 0 && x < 5)
			return m_bottomCells[x];
		return null;
	}

	public Cell GetCell(int x, int y)
	{
		if (x >= 0 && x < boardSizeX && y >= 0 && y < boardSizeY)
			return m_cells[x, y];
		return null;
	}
}