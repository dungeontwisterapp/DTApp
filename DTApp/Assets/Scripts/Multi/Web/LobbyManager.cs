using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Multi
{
    namespace Web
    {

        public class LobbyManager
        {
            public enum ActionType { NONE, PLAY, JOIN, WATCH, QUIT, CREATE }

            public LobbyManager()
            {
                buttons = null;
                _tableList = new BGA.TableListData();
            }

            public bool IsActive { get { return buttons != null; } }
            public BGA.TableListData Tables { get { return _tableList; } }

            public void UpdateTables(JSONObject tables)
            {
                _tableList.Update(tables);
                GameList.SetDirty();
            }

            public void RegisterActionButtons(GameObject buttonAction1, GameObject buttonAction2)
            {
                Debug.Assert(!IsActive);
                buttons = new List<GameObject>();
                buttons.Add(buttonAction1);
                buttons.Add(buttonAction2);
            }

            public void UnRegisterActionButtons()
            {
                Debug.Assert(IsActive);
                buttons = null;
            }

            public string SelectedTable
            {
                get { return _selectedTable; }
                set
                {
                    _selectedTable = value;
                    UpdateActionButtons();
                }
            }

            public void UpdateActionButtons()
            {
                for (int i = 0; i < 2; ++i)
                {
                    switch (EvaluateAction(i))
                    {
                        case ActionType.CREATE:
                            buttons[i].transform.FindChild("Text").gameObject.GetComponent<Text>().text = "NEW GAME";
                            buttons[i].SetActive(true);
                            break;
                        case ActionType.JOIN:
                            buttons[i].transform.FindChild("Text").gameObject.GetComponent<Text>().text = "JOIN GAME";
                            buttons[i].SetActive(true);
                            break;
                        case ActionType.PLAY:
                            buttons[i].transform.FindChild("Text").gameObject.GetComponent<Text>().text = "PLAY GAME";
                            buttons[i].SetActive(true);
                            break;
                        case ActionType.WATCH:
                            buttons[i].transform.FindChild("Text").gameObject.GetComponent<Text>().text = "WATCH GAME";
                            buttons[i].SetActive(true);
                            break;
                        case ActionType.QUIT:
                            buttons[i].transform.FindChild("Text").gameObject.GetComponent<Text>().text = "QUIT GAME";
                            buttons[i].SetActive(true);
                            break;
                        case ActionType.NONE:
                            buttons[i].SetActive(false);
                            break;
                    }
                }
            }

            public void Action(int i)
            {
                switch (EvaluateAction(i))
                {
                    case ActionType.CREATE:
                        Http.Instance.CreateGame();
                        break;
                    case ActionType.JOIN:
                        Http.Instance.JoinGame(_selectedTable);
                        break;
                    case ActionType.PLAY:
                    case ActionType.WATCH:
                        Http.Instance.WatchGame(_selectedTable);
                        break;
                    case ActionType.QUIT:
                        Http.Instance.QuitGame(_selectedTable);
                        break;
                    case ActionType.NONE:
                        break;
                }
            }

            // implementation details
            private List<GameObject> buttons;
            private BGA.TableListData _tableList;
            private string _selectedTable = "";

            private ActionType EvaluateAction(int action)
            {
                Debug.Assert(IsActive);
                if (action < 0 || action > 1)
                {
                    return ActionType.NONE;
                }

                ActionType[] actions = { ActionType.NONE, ActionType.NONE };

                if (_selectedTable == "") // no table selected
                {
                    actions[1] = ActionType.CREATE;
                }
                else
                {
                    BGA.TableData table = _tableList[_selectedTable];
                    switch (table.playerCount)
                    {
                        case 2:
                            if (table.HasPlayer(Http.Instance.BgaUserId))
                            {
                                actions[0] = ActionType.PLAY;
                                actions[1] = ActionType.QUIT;
                            }
                            else
                            {
                                actions[1] = ActionType.WATCH;
                            }
                            break;
                        case 1:
                            if (table.HasPlayer(Http.Instance.BgaUserId))
                            {
                                actions[1] = ActionType.QUIT;
                            }
                            else
                            {
                                actions[1] = ActionType.JOIN;
                            }
                            break;
                        case 0:
                        default:
                            break;
                    }
                }

                return actions[action];
            }
        }

    }
}
