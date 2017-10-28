using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Multi
{

    public class GameList : MonoBehaviour
    {
        public int height = 30;

        public int _selection = -1;

        GameObject _content = null;
        GameObject _lineModel = null;
        DateTime _nextChange;
        TimeSpan _refreshTimeSpan;

        List<GameObject> _buttonList = null;
        List<string> _keys;

        // all this static stuff is dirty => to be changed
        static private bool _change = false;
        public static void SetDirty()
        {
            _change = true;
        }
        
        public string GetSelectedGameId()
        {
            return _selection == -1 ? "" : _keys[_selection];
        }

        // Use this for initialization
        void Start()
        {
            _refreshTimeSpan = new TimeSpan(0, 0, 0, 10); // 10sec before automatic refresh
            _nextChange = DateTime.Now;
            _change = false;
            _lineModel = GameObject.Find("GameLine");
            _content = _lineModel.transform.parent.gameObject;
            _lineModel.SetActive(false);
            _buttonList = new List<GameObject>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_change)
            {
                Logger.Instance.Log("DETAIL", "change");
                Reload();
            }
            else if (DateTime.Now  >= _nextChange)
            {
                Logger.Instance.Log("NW_INFO", "ask to refresh");
                if (Http.Instance.LoadTables(true))
                {
                    Logger.Instance.Log("DETAIL", "ask success");
                    _nextChange = DateTime.Now + _refreshTimeSpan; 
                }
            }
        }

        private void Reload()
        {
            string selectedId = GetSelectedGameId();
            Logger.Instance.Log("DETAIL", "previously selected id: " + selectedId);
            OnGameSelected(-1);

            for (int i = 0; i < _buttonList.Count; ++i)
            {
                GameObject.Destroy(_buttonList[i]);
            }
            _buttonList.Clear();
            _change = false;
            _nextChange = DateTime.Now + _refreshTimeSpan;
            
            BGA.TableListData tables = Http.Instance.Lobby.Tables;
            _keys = tables.GetSortedKeys();

            GenerateDefaultLines(tables.Count);

            for (int i = 0; i < tables.Count; ++i)
            {
                string id = _keys[i];
                BGA.TableData data = tables[id];

                string players = "";
                if (data.player1 != null)
                {
                    players += data.player1.fullname + "(" + data.player1.rank + ")";
                }
                if (data.player2 != null)
                {
                    players += " - " + data.player2.fullname + "(" + data.player2.rank + ")";
                }

                string status = "";
                if (data.isOpen && data.playerCount == 1)
                {
                    if (data.HasPlayer(Http.Instance.BgaUserId)) status = "En attente";
                    else status = "Rejoindre";
                }
                else if (data.playerCount == 2)
                {
                    if (data.HasPlayer(Http.Instance.BgaUserId)) status = "Reprendre";
                    else status = "Observer";
                }
                else
                {
                    status = "Invalide";
                }

                GameObject line = _buttonList[i];
                line.transform.FindChild("Joueurs").gameObject.GetComponent<Text>().text = players;
                line.transform.FindChild("Etat").gameObject.GetComponent<Text>().text = status;

                Logger.Instance.Log("DETAIL", "compare ids " + id + " and " + selectedId);
                if (id == selectedId)
                {
                    Logger.Instance.Log("DETAIL", "newly selected id: " + selectedId);
                    OnGameSelected(i);
                }
            }
            
        }

        private void GenerateDefaultLines(int n)
        {
            Vector2 d = _content.GetComponent<RectTransform>().sizeDelta;
            d.y = height * (n == 0 ? 1 : n);
            _content.GetComponent<RectTransform>().sizeDelta = d;

            for (int i = 0; i < n; ++i)
            {
                GameObject newline = GameObject.Instantiate(_lineModel) as GameObject;
                newline.transform.SetParent(_content.transform, false);
                Vector3 position = newline.transform.localPosition;
                position.y -= height * i;
                newline.transform.localPosition = position;
                newline.SetActive(true);
                int j = i;
                newline.GetComponent<Button>().onClick.AddListener(delegate { OnGameSelected(j); });
                _buttonList.Add(newline);
            }
        }

        public void OnGameSelected(int index)
        {
            if (_selection != -1)
            {
                ColorBlock colors = _buttonList[_selection].GetComponent<Button>().colors;
                colors.normalColor = new Color(1, 1, 1, 0);
                colors.highlightedColor = new Color(1, 1, 1, 0.25f);
                colors.pressedColor = new Color(1, 1, 1, 0.75f);
                _buttonList[_selection].GetComponent<Button>().colors = colors;
            }

            if (_selection == index || index == -1)
            {
                _selection = -1;
            }
            else
            {
                _selection = index;

                ColorBlock colors = _buttonList[_selection].GetComponent<Button>().colors;
                colors.normalColor = new Color(0, 1, 0, 0.25f);
                colors.highlightedColor = new Color(0, 1, 0, 0.5f);
                colors.pressedColor = new Color(0, 1, 0, 0.75f);
                _buttonList[_selection].GetComponent<Button>().colors = colors;
            }
            
            Http.Instance.Lobby.SelectedTable = GetSelectedGameId();
        }
    }

}
