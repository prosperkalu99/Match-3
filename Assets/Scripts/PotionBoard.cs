using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PotionBoard : MonoBehaviour
{

    //Define the size of the board
    public int width = 6;
    public int height = 8;

    //Define some space for the board
    public float spacingX;
    public float spacingY;

    //Get a reference to our potion prefabs
    public GameObject[] potionPrefabs;

    //Get a reference to the collection nodes portionBoard + GameObject
    public Node[,] potionBoard;
    public GameObject potionBoardGO;

    public List<GameObject> potionsToDestroy = new();
    public GameObject potionParent;

    //Layout Array
    public ArrayLayout arrayLayout;

    //Public static of potionBoard
    public static PotionBoard instance;

    [SerializeField]
    private Potion selectedPotion;

    [SerializeField]
    private bool isProcessingMove;

    [SerializeField]
    private List<Potion> potionsToRemove = new();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        InitializeBoard();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider != null && hit.collider.gameObject.GetComponent<Potion>())
            {
                if (isProcessingMove)
                {
                    return;
                }
                Potion potion = hit.collider.gameObject.GetComponent<Potion>();
                Debug.Log("I have clicked a potion, it is: " + potion.gameObject);

                SelectPotion(potion);
            }
        }
    }

    void InitializeBoard()
    {
        DestroyPotions();
        potionBoard = new Node[width, height];
        spacingX = ((width - 1) / 2) + 0.5f;
        spacingY = ((height - 1) / 2) + 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY);
                if (arrayLayout.rows[y].row[x])
                {
                    potionBoard[x, y] = new Node(false, null);
                }
                else
                {
                    int randomIndex = Random.Range(0, potionPrefabs.Length);

                    GameObject potion = Instantiate(potionPrefabs[randomIndex], position, Quaternion.identity);
                    potion.transform.SetParent(potionParent.transform);
                    potion.GetComponent<Potion>().SetIndices(x, y);
                    potionBoard[x, y] = new Node(true, potion);
                    potionsToDestroy.Add(potion);
                }
            }
        }

        if (CheckBoard())
        {
            Debug.Log("We have matches let's recreate the board");
            InitializeBoard();
        }
        else
        {
            Debug.Log("There are no matches, it's time to start the game!");
        };
    }

    private void DestroyPotions()
    {
        if (potionsToDestroy != null)
        {
            foreach (GameObject potion in potionsToDestroy)
            {
                Destroy(potion);
            }
            potionsToDestroy.Clear();
        }
    }

    public bool CheckBoard()
    {
        if (GameManager.Instance.isGameEnded)
        {
            return false;
        }

        Debug.Log("Checking Board");
        bool hasMatched = false;

        potionsToRemove.Clear();

        foreach (Node node in potionBoard)
        {
            if (node.portion != null)
            {
                node.portion.GetComponent<Potion>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Check if potion node is usable
                if (potionBoard[x, y].isUsable)
                {
                    // then proceed to get potion class in node
                    Potion potion = potionBoard[x, y].portion.GetComponent<Potion>();

                    //ensure it's not matched
                    if (!potion.isMatched)
                    {
                        // run some matching logic
                        MatchResult matchedPotions = IsConnected(potion);

                        if (matchedPotions.connectedPotions.Count >= 3)
                        {
                            //complex matching
                            MatchResult superMatchedPotions = SuperMatch(matchedPotions);

                            potionsToRemove.AddRange(superMatchedPotions.connectedPotions);

                            foreach (Potion pot in superMatchedPotions.connectedPotions)
                            {
                                pot.isMatched = true;
                            }
                            hasMatched = true;
                        }
                    }
                }
            }
        }
        return hasMatched;
    }

    public IEnumerator ProcessTurnOnMatchedBoard(bool _subtractMoves)
    {
        foreach (Potion potion in potionsToRemove)
        {
            potion.isMatched = false;
        }

        RemoveAndRefill(potionsToRemove);
        GameManager.Instance.ProcessTurn(potionsToRemove.Count, _subtractMoves);
        yield return new WaitForSeconds(0.4f);

        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(false));
        }
    }

    #region Matching Potions

    void CheckDirection(Potion pot, Vector2Int direction, List<Potion> connectedPotions)
    {
        PotionType potionType = pot.portionType;
        int x = pot.xIndex + direction.x;
        int y = pot.yIndex + direction.y;

        // check that we're within the boundaries of the board
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (potionBoard[x, y].isUsable)
            {
                Potion neighbourPotion = potionBoard[x, y].portion.GetComponent<Potion>();

                //Does our potionType match?
                if (!neighbourPotion.isMatched && neighbourPotion.portionType == potionType)
                {
                    connectedPotions.Add(neighbourPotion);

                    x += direction.x;
                    y += direction.y;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    MatchResult IsConnected(Potion potion)
    {
        List<Potion> connectedPotions = new();
        PotionType potionType = potion.portionType;

        connectedPotions.Add(potion);

        //check right  
        CheckDirection(potion, new Vector2Int(1, 0), connectedPotions);

        //check left
        CheckDirection(potion, new Vector2Int(-1, 0), connectedPotions);

        //have we made a 3 match? (Horizontal match)
        if (connectedPotions.Count == 3)
        {
            Debug.Log("I have a normal horizontal match, the color of my match is: " + connectedPotions[0].portionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Horizontal
            };
        }

        //checking for more than 3 match (Long Horizontal match)
        else if (connectedPotions.Count > 3)
        {
            Debug.Log("I have a long horizontal match, the color of my match is: " + connectedPotions[0].portionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongHorizontal
            };
        }

        //clear out the connected potions
        connectedPotions.Clear();

        //read our initial potion
        connectedPotions.Add(potion);

        //check up
        CheckDirection(potion, new Vector2Int(0, 1), connectedPotions);

        //check down
        CheckDirection(potion, new Vector2Int(0, -1), connectedPotions);

        //have we made a 3 match? (Vertical match)
        if (connectedPotions.Count == 3)
        {
            Debug.Log("I have a normal vertical match, the color of my match is: " + connectedPotions[0].portionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Vertical
            };
        }

        //checking for more than 3 match (Long Vertical match)
        else if (connectedPotions.Count > 3)
        {
            Debug.Log("I have a long vertical match, the color of my match is: " + connectedPotions[0].portionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongVertical
            };
        }
        else
        {
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.None
            };
        }
    }

    private MatchResult SuperMatch(MatchResult _matchedResults)
    {
        //if we have a horizontal or long horizontal match
        if (_matchedResults.direction == MatchDirection.Horizontal || _matchedResults.direction == MatchDirection.LongHorizontal)
        {
            // for each position...
            foreach (Potion pot in _matchedResults.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new();

                //check up
                CheckDirection(pot, new Vector2Int(0, 1), extraConnectedPotions);

                //checl down
                CheckDirection(pot, new Vector2Int(0, -1), extraConnectedPotions);

                //do we have 2 or more potions that has been matched against this current potion
                if (extraConnectedPotions.Count >= 2)
                {
                    Debug.Log("I have a super horizontal match");
                    extraConnectedPotions.AddRange(_matchedResults.connectedPotions);

                    //return a super match
                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super,
                    };
                }
            }

            // we didn't have a super match, return a normal match
            return new MatchResult
            {
                connectedPotions = _matchedResults.connectedPotions,
                direction = _matchedResults.direction,
            };
        }

        //if we have a vertical or long vertical match
        else if (_matchedResults.direction == MatchDirection.Vertical || _matchedResults.direction == MatchDirection.LongVertical)
        {
            // for each position...
            foreach (Potion pot in _matchedResults.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new();

                //check right
                CheckDirection(pot, new Vector2Int(1, 0), extraConnectedPotions);

                //checl left
                CheckDirection(pot, new Vector2Int(-1, 0), extraConnectedPotions);

                //do we have 2 or more potions that has been matched against this current potion
                if (extraConnectedPotions.Count >= 2)
                {
                    Debug.Log("I have a super vertical match");
                    extraConnectedPotions.AddRange(_matchedResults.connectedPotions);

                    //return a super match
                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super,
                    };
                }
            }

            // we didn't have a super match, return a normal match
            return new MatchResult
            {
                connectedPotions = _matchedResults.connectedPotions,
                direction = _matchedResults.direction,
            };
        }
        return null;
    }

    #endregion

    #region Swapping Potions

    // Select potion
    public void SelectPotion(Potion _potion)
    {
        // if we don't have a potion currently selected, then set the potion I just clicked to my selectedPotion
        if (selectedPotion == null)
        {
            Debug.Log(_potion);
            selectedPotion = _potion;
        }

        // if we select same potion twice, let's make selectPotion null
        else if (selectedPotion == _potion)
        {
            selectedPotion = null;
        }

        // if selectedPotion is not null and is not the current potion, attempt a swap
        // selectedPotion back to null
        else if (selectedPotion != _potion)
        {
            SwapPotion(selectedPotion, _potion);
            selectedPotion = null;
        }
    }

    // Swap potion - logic
    private void SwapPotion(Potion _currentPotion, Potion _targetPotion)
    {
        //!IsAdjacent don't do anything
        if (!IsAdjacent(_currentPotion, _targetPotion))
        {
            return;
        }

        //Do swap
        DoSwap(_currentPotion, _targetPotion);

        isProcessingMove = true;

        //StartCoroutine ProcessMatches
        StartCoroutine(ProcessMatches(_currentPotion, _targetPotion));

    }

    // do swap
    private void DoSwap(Potion _currentPotion, Potion _targetPotion)
    {
        GameObject temp = potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].portion;
        potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].portion = potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].portion;
        potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].portion = temp;

        //update indicies
        int tempXIndex = _currentPotion.xIndex;
        int tempYIndex = _currentPotion.yIndex;
        _currentPotion.xIndex = _targetPotion.xIndex;
        _currentPotion.yIndex = _targetPotion.yIndex;
        _targetPotion.xIndex = tempXIndex;
        _targetPotion.yIndex = tempYIndex;

        _currentPotion.MoveToTarget(potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].portion.transform.position);

        _targetPotion.MoveToTarget(potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].portion.transform.position);
    }

    //IsAdjacent 
    private bool IsAdjacent(Potion _currentPotion, Potion _targetPotion)
    {
        return Mathf.Abs(_currentPotion.xIndex - _targetPotion.xIndex) + Mathf.Abs(_currentPotion.yIndex - _targetPotion.yIndex) == 1;
    }

    //ProcessMatches
    private IEnumerator ProcessMatches(Potion _currentPotion, Potion _targetPotion)
    {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoard())
        {
            // start a coroutine that is going to process our matches in our turn
            StartCoroutine(ProcessTurnOnMatchedBoard(true));
        }
        else
        {
            DoSwap(_currentPotion, _targetPotion);
        }
        isProcessingMove = false;
    }

    #endregion

    #region Cascading Potions

    private void RemoveAndRefill(List<Potion> _potionsToRemove)
    {
        //removing the potion and clearing the board at the location
        foreach (Potion potion in _potionsToRemove)
        {
            //getting it's x and y indices and storing them
            int _xIndex = potion.xIndex;
            int _yIndex = potion.yIndex;

            // destroy the potion
            Destroy(potion.gameObject);

            //create a blank node on the potion board   
            potionBoard[_xIndex, _yIndex] = new Node(true, null);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (potionBoard[x, y].portion == null)
                    {
                        Debug.Log("The locatio of x: " + x + " y: " + y + "is empty, attempting to refill it");
                        RefillPotion(x, y);
                    }
                }
            }

        }

    }

    private void RefillPotion(int x, int y)
    {
        int yOffset = 1;

        //while the cell above our current cell is null and we're below the height of the board
        while (y + yOffset < height && potionBoard[x, y + yOffset].portion == null)
        {
            //Increment y offset
            Debug.Log("The potion above me is null, but I'm not at the top of the board yet. So add to my offset and try again. Current offset is: " + yOffset + "I'm about to add 1.");
            yOffset++;
        }

        // we've either hit the top of the poboardtion or found a potion
        if (y + yOffset < height && potionBoard[x, y + yOffset].portion != null)
        {
            // we've found a potion
            Potion potionAbove = potionBoard[x, y + yOffset].portion.GetComponent<Potion>();

            //Move it to the current location
            Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, potionAbove.transform.position.z);
            Debug.Log("I've found a potion when refilling the board and it was in the location: [" + x + "," + (y + yOffset) + "] We have moved it to the location: [" + x + "," + y + "]");

            //Move to location 
            potionAbove.MoveToTarget(targetPos);

            //update indicies
            potionAbove.SetIndices(x, y);

            //update our potionBoard
            potionBoard[x, y] = potionBoard[x, y + yOffset];

            //set the location the potion came from to null
            potionBoard[x, y + yOffset] = new Node(true, null);
        }

        // if we hit the top of the board without finding a potion
        if (y + yOffset == height)
        {
            Debug.Log("I've reached the top of the board without finding a potion");
            SpawnPotionAtTop(x);
        }
    }

    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = 99;
        for (int y = 7; y >= 0; y--)
        {
            if (potionBoard[x, y].portion == null)
            {
                lowestNull = y;
            }
        }
        return lowestNull;
    }

    private void SpawnPotionAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x);
        int locationToMoveTo = 8 - index;
        Debug.Log("About to spawn a potion, Ideally, I'd like to put it in the index of: " + index);

        //get a random potion
        int randomIndex = Random.Range(0, potionPrefabs.Length);
        GameObject newPotion = Instantiate(potionPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
        newPotion.transform.SetParent(potionParent.transform);

        // set indicies
        newPotion.GetComponent<Potion>().SetIndices(x, index);

        // set it on the potion board
        potionBoard[x, index] = new Node(true, newPotion);

        //move it to that location
        Vector3 targetPos = new Vector3(newPotion.transform.position.x, newPotion.transform.position.y - locationToMoveTo, newPotion.transform.position.z);
        newPotion.GetComponent<Potion>().MoveToTarget(targetPos);
    }

    #endregion
}

public class MatchResult
{
    public List<Potion> connectedPotions;
    public MatchDirection direction;
}

public enum MatchDirection
{
    Vertical,
    Horizontal,
    LongVertical,
    LongHorizontal,
    Super,
    None
}
