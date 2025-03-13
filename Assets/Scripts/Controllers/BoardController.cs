using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

	private NormalItem.eNormalType? m_lastItemType; // Theo dõi loại vật phẩm đã chọn trước đó cho AUTOPLAY

	public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        //FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                //StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

       /* if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                ShowHint();
            }
        }*/

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                m_isDragging = true;
                m_hitCollider = hit.collider;
            }
        }

		//Xử lý chạm (tap) để di chuyển vật phẩm
		if (Input.GetMouseButtonDown(0))
		{
			var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
			if (hit.collider != null)
			{
				Cell tappedCell = hit.collider.GetComponent<Cell>();
				// Chỉ xử lý nếu ô được chạm thuộc bảng chính (BoardY >= 0)
				if (tappedCell != null && !tappedCell.IsEmpty && tappedCell.BoardY >= 0)
				{
					IsBusy = true;
					MoveItemToBottom(tappedCell);
				}
				else if (tappedCell != null && !tappedCell.IsEmpty && tappedCell.BoardY < 0)
				{
					if (m_gameManager.canReturn)
					{
						Debug.Log("Return");
						/*m_board.m_cells[tappedCell.Item.InitialBoardX, tappedCell.Item.InitialBoardY].Assign(tappedCell.Item);
						tappedCell.Free();*/
					}
						
				}
			}
		}

		/*if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        if (Input.GetMouseButton(0) && m_isDragging)
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                if (m_hitCollider != null && m_hitCollider != hit.collider)
                {
                    StopHints();

                    Cell c1 = m_hitCollider.GetComponent<Cell>();
                    Cell c2 = hit.collider.GetComponent<Cell>();
                    if (AreItemsNeighbor(c1, c2))
                    {
                        IsBusy = true;
                        SetSortingLayer(c1, c2);
                        m_board.Swap(c1, c2, () =>
                        {
                            FindMatchesAndCollapse(c1, c2);
                        });

                        ResetRayCast();
                    }
                }
            }
            else
            {
                ResetRayCast();
            }
        }*/
	}

	private void MoveItemToBottom(Cell cell)
	{
		// In trạng thái bảng phụ trước khi tìm ô trống
		Debug.Log("Checking bottom row state:");
		for (int x = 0; x < 5; x++) // Giới hạn ở 5 ô
		{
			Cell bottomCell = m_board.GetBottomCell(x);
			Debug.Log($"Bottom cell {x}: {(bottomCell.IsEmpty ? "Empty" : "Occupied")}");
		}

		// Tìm ô trống đầu tiên trong bảng phụ từ trái sang phải
		Cell targetCell = null;
		for (int x = 0; x < 5; x++) // Giới hạn ở 5 ô
		{
			Cell bottomCell = m_board.GetBottomCell(x);
			if (bottomCell.IsEmpty)
			{
				targetCell = bottomCell;
				break;
			}
		}

		// Nếu không tìm thấy ô trống, bảng phụ đã đầy, thua ngay lập tức
		if (targetCell == null)
		{
			Debug.Log("No empty cell found in bottom row, game over (Lose)");
			m_gameManager.GameOver(false); // Thua ngay lập tức
			IsBusy = false; // Đặt lại IsBusy để tránh khóa game
			return;
		}

		// Di chuyển vật phẩm từ bảng chính xuống ô trống trong bảng phụ
		Item item = cell.Item;
		cell.Free();
		targetCell.Assign(item);
		item.View.DOMove(targetCell.transform.position, 0.3f).OnComplete(() =>
		{
			OnMoveEvent();
			CheckBottomRowForMatches(); // Kiểm tra bảng phụ sau khi di chuyển
		});
	}

	private void CheckBottomRowForMatches()
	{
		// Tạo dictionary để đếm số lượng mỗi loại vật phẩm trong bảng phụ
		Dictionary<NormalItem.eNormalType, List<Cell>> itemTypes = new Dictionary<NormalItem.eNormalType, List<Cell>>();

		// Duyệt qua tất cả ô trong bảng phụ (giới hạn 5 ô)
		for (int x = 0; x < 5; x++)
		{
			Cell cell = m_board.GetBottomCell(x);
			if (cell.IsEmpty || cell.Item == null) continue;

			// Chỉ xử lý nếu vật phẩm là NormalItem
			if (cell.Item is NormalItem normalItem)
			{
				NormalItem.eNormalType itemType = normalItem.ItemType;
				if (!itemTypes.ContainsKey(itemType))
				{
					itemTypes[itemType] = new List<Cell>();
				}
				itemTypes[itemType].Add(cell);
			}
		}

		// Kiểm tra và xóa nếu có ít nhất 3 vật phẩm cùng loại
		List<Cell> cellsToClear = new List<Cell>();
		foreach (var kvp in itemTypes)
		{
			if (kvp.Value.Count >= 3)
			{
				Debug.Log($"Found 3 or more items of type {kvp.Key}, clearing them");
				cellsToClear.AddRange(kvp.Value.Take(3)); // Lấy 3 ô đầu tiên để xóa
				break; // Chỉ xóa một nhóm 3 lần đầu tiên tìm thấy
			}
		}

		// Xóa các vật phẩm được chọn
		foreach (var cell in cellsToClear)
		{
			cell.ExplodeItem();
		}

		// Chỉ dồn nếu có ít nhất một nhóm được xóa
		if (cellsToClear.Count > 0)
		{
			// Lấy danh sách các ô còn vật phẩm
			List<Cell> remainingCells = new List<Cell>();
			for (int x = 0; x < 5; x++) // Giới hạn ở 5 ô
			{
				Cell cell = m_board.GetBottomCell(x);
				if (!cell.IsEmpty && !cellsToClear.Contains(cell))
				{
					remainingCells.Add(cell);
				}
			}

			// Sắp xếp remainingCells để không trùng loại liền kề (nếu có thể)
			List<Cell> sortedRemainingCells = new List<Cell>();
			Dictionary<NormalItem.eNormalType, Queue<Cell>> typeQueues = new Dictionary<NormalItem.eNormalType, Queue<Cell>>();

			foreach (var cell in remainingCells)
			{
				if (cell.Item is NormalItem normalItem)
				{
					NormalItem.eNormalType itemType = normalItem.ItemType;
					if (!typeQueues.ContainsKey(itemType))
					{
						typeQueues[itemType] = new Queue<Cell>();
					}
					typeQueues[itemType].Enqueue(cell);
				}
			}

			while (typeQueues.Values.Any(q => q.Count > 0))
			{
				foreach (var queue in typeQueues.Values.Where(q => q.Count > 0))
				{
					if (queue.Count > 0)
					{
						sortedRemainingCells.Add(queue.Dequeue());
					}
				}
			}

			// Dồn vật phẩm bằng coroutine
			StartCoroutine(ShiftItemsToLeft(sortedRemainingCells));
		}

		// Sau khi dồn (nếu có), kiểm tra điều kiện thắng/thua
		IsBusy = false;
		StartCoroutine(CheckWinLoseCondition());
	}

	private IEnumerator ShiftItemsToLeft(List<Cell> sortedRemainingCells)
	{
		IsBusy = true; // Ngăn người chơi thao tác trong khi dồn

		for (int x = 0; x < 5; x++) // Giới hạn ở 5 ô
		{
			Cell targetCell = m_board.GetBottomCell(x);
			if (x < sortedRemainingCells.Count && sortedRemainingCells[x].Item != null)
			{
				Item item = sortedRemainingCells[x].Item;
				targetCell.Assign(item); // Gán trước để đảm bảo vật phẩm sử dụng được
				item.View.DOMove(targetCell.transform.position, 0.3f).OnComplete(() =>
				{
					sortedRemainingCells[x].Free(); // Giải phóng ô cũ sau khi animation hoàn tất
					Debug.Log($"Moved item of type {((NormalItem)item).ItemType} to column {x}");
				});
			}
			else
			{
				targetCell.Free(); // Đặt các ô còn lại thành trống
				Debug.Log($"Freed column {x}");
			}
		}

		yield return new WaitForSeconds(0.3f); // Chờ animation hoàn tất

		// Kiểm tra lại bảng phụ ngay sau khi dồn
		bool bottomRowFull = true;
		for (int x = 0; x < 5; x++) // Giới hạn ở 5 ô
		{
			Cell cell = m_board.GetBottomCell(x);
			if (cell.IsEmpty)
			{
				bottomRowFull = false;
				break;
			}
		}

		if (bottomRowFull)
		{
			Debug.Log("Bottom row full after shifting, game over (Lose)");
			m_gameManager.GameOver(false); // Thua ngay lập tức
		}

		IsBusy = false; // Cho phép người chơi tiếp tục sau khi dồn
	}
	// Thêm: Phương thức kiểm tra điều kiện thắng/thua
	private IEnumerator CheckWinLoseCondition()
	{
		yield return new WaitForSeconds(0.3f); // Chờ animation xóa hoàn tất

		// Kiểm tra thua: Tất cả ô trong bảng phụ đều có vật phẩm
		bool bottomRowFull = true;
		for (int x = 0; x < 5; x++) // Giới hạn ở 5 ô
		{
			Cell cell = m_board.GetBottomCell(x);
			if (cell == null || cell.IsEmpty) // Thêm kiểm tra null để tránh lỗi
			{
				bottomRowFull = false;
				break;
			}
		}

		if (bottomRowFull)
		{
			Debug.Log("Bottom row is full, game over (Lose)");
			m_gameManager.GameOver(false); // Thua
			yield break;
		}

		// Kiểm tra thắng: Toàn bộ bảng chính trống
		bool boardEmpty = true;
		for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
		{
			for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
			{
				Cell cell = m_board.GetCell(x, y);
				if (cell == null || !cell.IsEmpty) // Thêm kiểm tra null để tránh lỗi
				{
					boardEmpty = false;
					break;
				}
			}
		}

		if (boardEmpty)
		{
			Debug.Log("Main board is empty, game over (Win)");
			m_gameManager.GameOver(true); // Thắng
			yield break;
		}

		IsBusy = false;
	}

	public void StartAutoplay(bool aimToWin)
	{
		m_lastItemType = null; // Reset loại vật phẩm trước đó khi bắt đầu
		StartCoroutine(AutoplayCoroutine(aimToWin));
	}

	private IEnumerator AutoplayCoroutine(bool aimToWin)
	{
		while (!m_gameOver)
		{
			if (IsBusy) yield return null; // Chờ nếu bảng đang bận

			Cell cellToMove = null;

			if (aimToWin) // Chơi để thắng
			{
				// Bước 1: Nếu chưa có loại vật phẩm, chọn loại đầu tiên có sẵn trên bảng chính
				if (m_lastItemType == null)
				{
					for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
					{
						for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
						{
							Cell cell = m_board.GetCell(x, y);
							if (!cell.IsEmpty && cell.Item is NormalItem item)
							{
								m_lastItemType = item.ItemType; // Chọn loại đầu tiên tìm thấy
								Debug.Log($"Selected initial item type: {m_lastItemType}");
								break;
							}
						}
						if (m_lastItemType.HasValue) break;
					}
				}

				// Bước 2: Chỉ chọn ô có cùng loại với m_lastItemType
				if (m_lastItemType.HasValue)
				{
					for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
					{
						for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
						{
							Cell cell = m_board.GetCell(x, y);
							if (!cell.IsEmpty && cell.Item is NormalItem item && item.ItemType == m_lastItemType)
							{
								cellToMove = cell;
								Debug.Log($"Selected cell with type {m_lastItemType} at ({x}, {y})");
								break;
							}
						}
						if (cellToMove != null) break;
					}

					// Bước 3: Nếu không còn ô nào cùng loại, chọn loại mới
					if (cellToMove == null)
					{
						for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
						{
							for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
							{
								Cell cell = m_board.GetCell(x, y);
								if (!cell.IsEmpty && cell.Item is NormalItem item && item.ItemType != m_lastItemType)
								{
									m_lastItemType = item.ItemType; // Chuyển sang loại mới
									cellToMove = cell;
									Debug.Log($"No more items of type {m_lastItemType}, switched to new type: {m_lastItemType}");
									break;
								}
							}
							if (cellToMove != null) break;
						}
					}
				}
			}
			else // Chơi để thua
			{
				// Ưu tiên chọn ô làm đầy bảng phụ, tránh tạo nhóm 3
				for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
				{
					for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
					{
						Cell cell = m_board.GetCell(x, y);
						if (!cell.IsEmpty && cell.Item is NormalItem item)
						{
							int countInBottom = 0;
							for (int bx = 0; bx < 5; bx++)
							{
								Cell bottomCell = m_board.GetBottomCell(bx);
								if (bottomCell != null && !bottomCell.IsEmpty && bottomCell.Item is NormalItem bottomItem && bottomItem.ItemType == item.ItemType)
								{
									countInBottom++;
								}
							}
							if (countInBottom < 2) // Tránh tạo nhóm 3
							{
								cellToMove = cell;
								m_lastItemType = item.ItemType;
								Debug.Log($"Selected cell with type {m_lastItemType} to avoid match");
								break;
							}
						}
					}
					if (cellToMove != null) break;
				}

				// Nếu không tìm thấy ô an toàn, chọn ngẫu nhiên
				if (cellToMove == null)
				{
					for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
					{
						for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
						{
							Cell cell = m_board.GetCell(x, y);
							if (!cell.IsEmpty)
							{
								cellToMove = cell;
								if (cell.Item is NormalItem item) m_lastItemType = item.ItemType;
								Debug.Log($"No safe move, selected random cell with type {m_lastItemType}");
								break;
							}
						}
						if (cellToMove != null) break;
					}
				}
			}

			// Thực hiện di chuyển nếu tìm thấy ô
			if (cellToMove != null)
			{
				IsBusy = true;
				MoveItemToBottom(cellToMove); // Di chuyển vật phẩm xuống bảng phụ
				yield return new WaitForSeconds(0.5f); // Chờ animation hoàn tất
			}
			else
			{
				Debug.LogWarning("No cell to move found, game might be over or board is empty.");
				yield return null;
			}
		}
	}

	/*private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }*/

	internal void Clear()
    {
        m_board.Clear();
    }

   /* private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }*/
}
