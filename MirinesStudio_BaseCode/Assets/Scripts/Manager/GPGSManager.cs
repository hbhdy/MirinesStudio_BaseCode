//using Cysharp.Threading.Tasks;
//using GooglePlayGames;
//using GooglePlayGames.BasicApi;
//using GooglePlayGames.BasicApi.SavedGame;
//using System;
//using System.Text;
//using System.Threading;
//using UnityEngine;
//using UnityEngine.SocialPlatforms;

//namespace MirinesStudio
//{
//    public class GPGSManager : BaseManager
//    {
//        // ----- Param -----

//        // 에디터 및 실패 폴백용
//        private const string GuestKey = "GPGS_GUEST_ID";

//        // fileName은 유저마다 고유한 파일명
//        public string fileName = string.Empty;

//        private string displayName { get; set; }
//        private string userId { get; set; }


//        public override async UniTask Task_Init()
//        {
//            PlayGamesPlatform.DebugLogEnabled = true;
//            PlayGamesPlatform.Activate();

//            // 로그인 완료까지 대기
//            var ok = await EnsureSignedInAsync(timeoutMs: 10000);

//            // 파일명 기본값 세팅(유저별 고유)
//            //if (string.IsNullOrEmpty(fileName))
//            //    fileName = $"save_{SanitizeForFilename(userId)}.dat";

//            await base.Task_Init();
//        }

//        public override void Init()
//        {
//            base.Init();
//        }

//        // 로그인 보장 함수
//        private async UniTask<bool> EnsureSignedInAsync(int timeoutMs = 10000, CancellationToken ct = default)
//        {
//#if UNITY_ANDROID && !UNITY_EDITOR
//            // 이미 인증되어 있으면 즉시 반환
//            if (Social.localUser != null && Social.localUser.authenticated)
//            {
//                displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
//                userId = PlayGamesPlatform.Instance.GetUserId();
//                return true;
//            }

//            var tcs = new UniTaskCompletionSource<bool>();

//            PlayGamesPlatform.Instance.Authenticate(status =>
//            {
//                if (status == SignInStatus.Success && Social.localUser.authenticated)
//                {
//                    displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
//                    userId = PlayGamesPlatform.Instance.GetUserId();
//                    tcs.TrySetResult(true);

//                }
//                else
//                {
//                    tcs.TrySetResult(false);
//                }
//            });

//            try
//            {
//                var result = await tcs.Task.AttachExternalCancellation(ct)
//                                           .Timeout(TimeSpan.FromMilliseconds(timeoutMs));
//                if (!result)
//                {
//                    UseEditorOrGuestFallback(); // 실패 시 게스트
//                }

//                return result;
//            }
//            catch // 타임아웃/취소
//            {
//                UseEditorOrGuestFallback();
//                return false;
//            }
//#else
//            UseEditorOrGuestFallback();
//            return false;
//#endif
//        }

//        private void UseEditorOrGuestFallback()
//        {
//#if UNITY_EDITOR
//            displayName = EditorGuestIdentity.GetDisplayName();
//            userId = EditorGuestIdentity.GetGuestId();
//#else
//            // 런타임 게스트(1회 생성 후 캐시)
//            string id = string.Empty;
//            GameConfig.Get(GameConfig.GUEST_KEY, ref id);

//            if (string.IsNullOrEmpty(id))
//            {
//                id = "guest_" + Guid.NewGuid().ToString("N").Substring(0, 16);
//                GameConfig.Set(GameConfig.GUEST_KEY, id);
//            }

//            userId = id;
//            displayName = "Guest";
//#endif
//        }

//        private string SanitizeForFilename(string s)
//        {
//            if (string.IsNullOrEmpty(s))
//                return "guest";
//            // 파일명에 안전하지 않은 문자 제거/치환
//            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
//                s = s.Replace(c, '_');
//            return s.Replace("/", "_");
//        }

//        // ----- Set -----

//        public string GetDisplayName() => displayName;
//        public string GetUserId() => userId;

//        // ----- Get -----

//        // ----- Main -----

//        // ----- Logic -----

//        #region 데이터 저장
//        public void SaveData() // 외부에서 접근할 함수
//        {
//            OpenSaveGame();
//        }

//        // fileName은 유저마다 고유한 파일명
//        private void OpenSaveGame()
//        {
//            ISavedGameClient saveGameClient = PlayGamesPlatform.Instance.SavedGame;

//            // 데이터 접근
//            saveGameClient.OpenWithAutomaticConflictResolution(fileName,
//                DataSource.ReadCacheOrNetwork,
//                ConflictResolutionStrategy.UseLastKnownGood,
//                onSavedGameOpend);
//        }

//        private void onSavedGameOpend(SavedGameRequestStatus status, ISavedGameMetadata game)
//        {
//            ISavedGameClient saveGameClient = PlayGamesPlatform.Instance.SavedGame;

//            if (status == SavedGameRequestStatus.Success)
//            {
//                var update = new SavedGameMetadataUpdate.Builder().Build();

//                //json
//                var json = JsonUtility.ToJson("저장하려는 데이터!");
//                byte[] data = Encoding.UTF8.GetBytes(json);

//                // 저장 함수 실행
//                saveGameClient.CommitUpdate(game, update, data, OnSavedGameWritten);
//            }
//            else
//            {
//                Debug.Log("Save No.....");
//            }
//        }

//        // 저장 확인 
//        private void OnSavedGameWritten(SavedGameRequestStatus status, ISavedGameMetadata data)
//        {
//            if (status == SavedGameRequestStatus.Success)
//            {
//                // 저장완료부분
//                Debug.Log("Save End");
//            }
//            else
//            {
//                Debug.Log("Save nonononononono...");
//            }
//        }
//        #endregion 데이터 저장

//        #region 데이터 로드
//        public void LoadData()
//        {
//            OpenLoadGame();
//        }

//        private void OpenLoadGame()
//        {
//            ISavedGameClient saveGameClient = PlayGamesPlatform.Instance.SavedGame;

//            saveGameClient.OpenWithAutomaticConflictResolution(fileName,
//                DataSource.ReadCacheOrNetwork,
//                ConflictResolutionStrategy.UseLastKnownGood,
//                LoadGameData);
//        }

//        private void LoadGameData(SavedGameRequestStatus status, ISavedGameMetadata data)
//        {
//            ISavedGameClient saveGameClient = PlayGamesPlatform.Instance.SavedGame;

//            if (status == SavedGameRequestStatus.Success)
//            {
//                Debug.Log("!! GoodLee");

//                // 데이터 로드
//                saveGameClient.ReadBinaryData(data, onSavedGameDataRead);
//            }
//            else
//            {
//                Debug.Log("?? no");
//            }
//        }

//        // 불러온 데이터 처리 
//        private void onSavedGameDataRead(SavedGameRequestStatus status, byte[] loadedData)
//        {
//            string data = System.Text.Encoding.UTF8.GetString(loadedData);

//            if (data == "")
//            {
//                SaveData();
//            }
//            else
//            {
//                // 불러온 데이터를 따로 처리해주는 부분 필요!
//            }
//        }

//        #endregion 데이터 로드

//        #region 데이터 삭제

//        public void DeleteData()
//        {
//            DeleteGameData();
//        }

//        private void DeleteGameData()
//        {
//            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;

//            savedGameClient.OpenWithAutomaticConflictResolution(fileName,
//                DataSource.ReadCacheOrNetwork,
//                ConflictResolutionStrategy.UseLastKnownGood,
//                DeleteSaveGame);
//        }


//        private void DeleteSaveGame(SavedGameRequestStatus status, ISavedGameMetadata data)
//        {
//            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;

//            if (status == SavedGameRequestStatus.Success)
//            {
//                savedGameClient.Delete(data);

//            }
//            else
//            {
//            }
//        }

//        #endregion 데이터 삭제

//        /*

//        #region 리더보드
//        public void ShowLeaderboardUI(int leaderBoardType)
//        {
//            string leaderBoardid = string.Empty;
//            if (leaderBoardType == 1)
//                leaderBoardid = GPGSIds.leaderboard__1;
//            else
//                leaderBoardid = GPGSIds.leaderboard__2;

//            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderBoardid);
//        }

//        public void UpdateScore(int leaderBoardType, int score)
//        {
//            string leaderBoardid = string.Empty;
//            if (leaderBoardType == 1)
//                leaderBoardid = GPGSIds.leaderboard__1;
//            else
//                leaderBoardid = GPGSIds.leaderboard__2;

//            PlayGamesPlatform.Instance.ReportScore(score, leaderBoardid, (bool success) => { });
//        }
//        #endregion 리더보드

//        #region 업적

//        public void ShowAchievementUI()
//        {
//            PlayGamesPlatform.Instance.ShowAchievementsUI();
//        }

//        // 단계별 업적 증가
//        public void IncrementGPGSAchievement()
//        {
//            PlayGamesPlatform.Instance.IncrementAchievement(GPGSIds.achievement___watching_commercials, 1, (bool success) => { });
//        }

//        // 업적 잠금 해제 및 공개
//        public void UnlockingGPGSAchievement()
//        {
//            PlayGamesPlatform.Instance.UnlockAchievement(GPGSIds.achievement___1_watching_commercials, (bool success) => { });
//        }

//        #endregion 업적

//        */
//    }
//}