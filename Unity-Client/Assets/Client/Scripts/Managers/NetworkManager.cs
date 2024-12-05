using Assambra.GameFramework.GameManager;
using com.tvd12.ezyfoxserver.client;
using com.tvd12.ezyfoxserver.client.constant;
using com.tvd12.ezyfoxserver.client.entity;
using com.tvd12.ezyfoxserver.client.factory;
using com.tvd12.ezyfoxserver.client.request;
using com.tvd12.ezyfoxserver.client.support;
using com.tvd12.ezyfoxserver.client.unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Assambra.Client
{
    public class NetworkManager : EzyAbstractController
    {
        public static NetworkManager Instance { get; private set; }

        private EzySocketConfig socketConfig;

        private bool _despawnInProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;

            socketConfig = GetSocketConfig();
        }

        private new void OnEnable()
        {
            base.OnEnable();

            AddHandler<EzyArray>(Commands.CHARACTER_LIST, CharacterListResponse);
            AddHandler<EzyObject>(Commands.PLAYER_SPAWN, ReceivePlayerSpawn);
            AddHandler<EzyObject>(Commands.PLAYER_DESPAWN, ReceivePlayerDespawn);
            AddHandler<EzyObject>(Commands.UPDATE_ENTITY_POSITION, ReceiveUpdateEntityPosition);
        }

        private void Update()
        {
            EzyClients.getInstance()
                .getClient(socketConfig.ZoneName)
                .processEvents();
        }

        protected override EzySocketConfig GetSocketConfig()
        {
            return EzySocketConfig.GetBuilder()
                .ZoneName("master-server")
                .AppName("master-server")
                .TcpUrl("127.0.0.1:3005")
                .UdpPort(2611)
                .UdpUsage(true)
                .EnableSSL(false)
                .Build();
        }

        public new void Disconnect()
        {
            base.Disconnect();
        }

        public void Login(string username, string password)
        {
            Debug.Log("Login username = " + username + ", password = " + password);
            Debug.Log("Socket clientName = " + socketProxy.getClient().getName());

            socketProxy.onLoginSuccess<Object>(LoginSuccessResponse);
            socketProxy.onUdpHandshake<Object>(UdpHandshakeResponse);
            socketProxy.onAppAccessed<Object>(AppAccessedResponse);

            // Login to socket server
            socketProxy.setLoginUsername(username);
            socketProxy.setLoginPassword(password);

            socketProxy.setUrl(socketConfig.TcpUrl);
            socketProxy.setUdpPort(socketConfig.UdpPort);
            socketProxy.setDefaultAppName(socketConfig.AppName);
            socketProxy.setTransportType(EzyTransportType.TCP);

            socketProxy.connect();
        }

        #region MASTER SERVER REQUESTS

        public void CharacterListRequest()
        {
            appProxy.send(Commands.CHARACTER_LIST);
        }

        public void CreateCharacterRequest(string name)
        {
            EzyObject characterdata = EzyEntityFactory
                .newObjectBuilder()
                .append("name", name)
                .build();

            appProxy.send(Commands.CREATE_CHARACTER, characterdata);
        }

        public void PlayRequest(long id)
        {
            EzyObject data = EzyEntityFactory
                .newObjectBuilder()
                .append("id", id)
                .build();

            appProxy.send(Commands.PLAY, data);
        }

        #endregion

        #region MASTER SERVER RESPONSE

        private void LoginSuccessResponse(EzySocketProxy proxy, Object data)
        {
            Debug.Log("Log in successfully");
        }

        private void UdpHandshakeResponse(EzySocketProxy proxy, Object data)
        {
            Debug.Log("HandleUdpHandshake");
            socketProxy.send(new EzyAppAccessRequest(socketConfig.AppName));
        }

        private void AppAccessedResponse(EzyAppProxy proxy, Object data)
        {
            Debug.Log("App access successfully");
            CharacterListRequest();
        }

        private void CharacterListResponse(EzyAppProxy proxy, EzyArray data)
        {
            if (data.isEmpty())
                GameManager.Instance.ChangeScene(Scenes.CreateCharacter);
            else
            {
                for (int i = 0; i < data.size(); i++)
                {
                    EzyObject characterInfo = data.get<EzyObject>(i);

                    CharacterInfoModel characterInfoModel = new CharacterInfoModel(
                        characterInfo.get<long>("id"),
                        characterInfo.get<string>("name"),
                        characterInfo.get<string>("room")
                        );

                    GameManager.Instance.CharacterInfos.Add(characterInfoModel);
                }

                GameManager.Instance.ChangeScene(Scenes.SelectCharacter);
            }
        }

        #endregion

        #region SEND TO ROOM SERVER

        public void SendPlayerInput(long id, string room, Vector3 input)
        {
            EzyArray inputArray = EzyEntityFactory.newArrayBuilder()
                .append(input.x)
                .append(input.z)
                .build();

            SendClientToServer(room, "playerInput", new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("id", id),
                new KeyValuePair<string, object>("room", room),
                new KeyValuePair<string, object>("input", inputArray)
            });
        }

        public void SendPlayerJump(long id, string room)
        {
            SendClientToServer(room, "playerJump", new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("id", id),
                new KeyValuePair<string, object>("room", room)
            });
        }

        #endregion

        #region RECEIVE FROM ROOM SERVER 

        private void ReceivePlayerSpawn(EzyAppProxy proxy, EzyObject data)
        {
            long id = data.get<long>("id");
            string name = data.get<string>("name");
            //Debug.Log($"Receive PLAYER_SPAWN request for {name}");

            bool isLocalPlayer = data.get<bool>("isLocalPlayer");
            string room = data.get<string>("room");
            EzyArray position = data.get<EzyArray>("position");
            EzyArray rotation = data.get<EzyArray>("rotation");
            Vector3 pos = new Vector3(position.get<float>(0), position.get<float>(1), position.get<float>(2));
            Vector3 rot = new Vector3(rotation.get<float>(0), rotation.get<float>(1), rotation.get<float>(2));

            if (!string.IsNullOrEmpty(room))
            {
                Scenes scenes = GameManager.Instance.getScenesByName(room);

                GameManager.Instance.ChangeScene(scenes);
            }

            StartCoroutine(WaitUntilDespawnDone(id, name, isLocalPlayer, room, pos, rot));
        }

        private IEnumerator WaitUntilDespawnDone(long id, string name, bool isLocalPlayer, string room, Vector3 position, Vector3 rotation)
        {
            yield return new WaitUntil(() => _despawnInProgress == false);

            GameObject playerGameObject = GameManager.Instance.CreatePlayer(position, rotation);
            playerGameObject.name = name;

            Player player = playerGameObject.GetComponent<Player>();
            PlayerController playerController = playerGameObject.GetComponent<PlayerController>();

            StartCoroutine(DelayToEnableCharacterController(playerController));

            if (playerController != null)
                playerController.Player = player;
            else
                Debug.LogError("PlayerController component not found on the playerGameObject.");

            if (player != null)
            {
                player.Initialize((uint)id, name, playerGameObject, room, isLocalPlayer);
                player.SetPlayerHeadinfoName(name);

                if (!isLocalPlayer)
                {
                    player.NetworkTransform.IsActive = true;
                    player.NetworkTransform.Initialize(position, Quaternion.Euler(rotation));
                }
                else
                {
                    GameManager.Instance.CameraController.ChangeCameraPreset("InGameCamera");
                    GameManager.Instance.CameraController.CameraTarget = playerGameObject;
                    GameManager.Instance.CameraController.Active = true;

                    player.NetworkTransform.IsActive = false;
                }

                GameManager.Instance.ClientEntities.Add((uint)id, player);
            }
            else
            {
                Debug.LogError("Player component not found on the playerGameObject.");
            }
        }

        private IEnumerator DelayToEnableCharacterController(PlayerController playerController)
        {
            yield return new WaitForSeconds(0.5f);

            playerController.CharacterController.enabled = true;
        }

        private void ReceivePlayerDespawn(EzyAppProxy proxy, EzyObject data)
        {
            _despawnInProgress = true;

            long id = data.get<long>("id");
            //Debug.Log($"Receive PLAYER_DESPAWN request for {id}");

            if (GameManager.Instance.ClientEntities.TryGetValue((uint)id, out Entity entity))
            {
                if (entity is Player player)
                {
                    if(player.IsLocalPlayer)
                    {
                        GameManager.Instance.CameraController.Active = false;
                        GameManager.Instance.CameraController.CameraTarget = null;

                        foreach(KeyValuePair<uint, Entity> e in GameManager.Instance.ClientEntities)
                        {
                            Destroy(e.Value.EntityGameObject);
                        }
                        GameManager.Instance.ClientEntities.Clear();
                    }
                    else
                    {
                        Destroy(player.EntityGameObject);
                        GameManager.Instance.ClientEntities.Remove(player.Id);
                    }
                }
            }

            _despawnInProgress = false;
        }

        private void ReceiveUpdateEntityPosition(EzyAppProxy proxy, EzyObject data)
        {
            long id = data.get<long>("id");
            EzyArray position = data.get<EzyArray>("position");
            EzyArray rotation = data.get<EzyArray>("rotation");

            Vector3 pos = new Vector3(position.get<float>(0), position.get<float>(1), position.get<float>(2));
            Vector3 rot = new Vector3(rotation.get<float>(0), rotation.get<float>(1), rotation.get<float>(2));

            if (GameManager.Instance.ClientEntities.TryGetValue((uint)id, out Entity entity))
            {
                if (entity.Id == id)
                {
                    if(entity is Player player)
                    {
                        if (!player.IsLocalPlayer)
                        {
                            entity.NetworkTransform.UpdateTargetPosition(pos);
                            entity.NetworkTransform.UpdateTargetRotation(Quaternion.Euler(rot));
                        }
                    }
                }
            }
            //Debug.Log($"Receive UPDATE_ENTITY_POSITION request Id: {id} ");
        }

        #endregion

        #region CLIENT TO ROOM SERVER MESSAGE

        private void SendClientToServer(string room, string command, List<KeyValuePair<string, object>> additionalParams)
        {
            //Debug.Log("SendClientToServer");

            var dataBuilder = EzyEntityFactory.newObjectBuilder()
                .append("room", room)
                .append("command", command);

            foreach (var pair in additionalParams)
            {
                dataBuilder.append(pair.Key, pair.Value);
            }

            EzyObject data = dataBuilder.build();

            appProxy.send(Commands.CLIENT_TO_SERVER, data);
        }

        #endregion
    }
}
