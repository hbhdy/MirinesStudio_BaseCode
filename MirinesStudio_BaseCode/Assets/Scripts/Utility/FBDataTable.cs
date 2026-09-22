//using Firebase.Firestore;

//namespace MirinesStudio
//{
//    // 파이어베이스 DB 테이블

//    [FirestoreData]
//    public class UserInfo
//    {
//        [FirestoreProperty]
//        public string ID { get; set; }

//        [FirestoreProperty]
//        public string Name { get; set; }

//        [FirestoreProperty]
//        public string Country { get; set; }

//        [FirestoreProperty, ServerTimestamp]
//        public Timestamp UpdateTime { get; set; } // 반드시 추가
//    }

//    [FirestoreData]
//    public class Payment
//    {
//        [FirestoreProperty]
//        public string UserID { get; set; }

//        [FirestoreProperty]
//        public string OrderID { get; set; }

//        [FirestoreProperty]
//        public string ProductID { get; set; }

//        [FirestoreProperty]
//        public string Country { get; set; }

//        [FirestoreProperty]
//        public float Price { get; set; }

//        [FirestoreProperty]
//        public int Status { get; set; }

//        [FirestoreProperty, ServerTimestamp]
//        public Timestamp PurchaseDate { get; set; }

//        [FirestoreProperty, ServerTimestamp]
//        public Timestamp UpdateTime { get; set; }
//    }

//    [FirestoreData]
//    public class BlackList
//    {
//        [FirestoreProperty]
//        public string UserID { get; set; }

//        [FirestoreProperty]
//        public int Type { get; set; }

//        [FirestoreProperty]
//        public Timestamp ExpireDate { get; set; }

//        [FirestoreProperty, ServerTimestamp]
//        public Timestamp UpdateTime { get; set; }

//        [FirestoreProperty]
//        public string Reason { get; set; }
//    }

//    [FirestoreData]
//    public class VersionInfo
//    {
//        [FirestoreProperty]
//        public string Current { get; set; }
//    }
//}