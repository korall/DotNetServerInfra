using System;
using System.Collections.Generic;
using Utility;

namespace AppSystem.Auth
{

    internal class Session
    {
        public DateTime StartTime { get; set; }

        public DateTime ValidityStartTime { get; set; }
        // 有效期，秒钟
        public long ValiditySec { get; set; }
        public object ContextObj { get; set; }

        public bool IsInValidityTime
        {
            get
            {
                if (ValiditySec < 0)
                    return true;
                if (ValiditySec > 0)
                {
                    var timeDiff = DateTime.UtcNow - ValidityStartTime;
                    if (timeDiff.TotalSeconds < ValiditySec)
                        return true;
                }
                return false;
            }
        }

        public int ValidityLease
        {
            get
            {
                if (ValiditySec < 0)
                    return -1;

                if (ValiditySec > 0)
                {
                    var timeDiff = DateTime.UtcNow - ValidityStartTime;
                    if (timeDiff.TotalSeconds < ValiditySec)
                        return (int)(ValiditySec - timeDiff.TotalSeconds);
                }

                return 0;
            }
        }
    }

    public class AreaSessionAuth : Singleton<AreaSessionAuth>
    {
        Dictionary<string, SessionAuth> mAllAreaSessionAuth = new Dictionary<string, SessionAuth>();

        public SessionAuth GetSessionAuth(string area)
        {
            lock (this)
            {
                if (mAllAreaSessionAuth.ContainsKey(area))
                    return mAllAreaSessionAuth[area];

                var sessionAuth = new SessionAuth();
                mAllAreaSessionAuth.Add(area, sessionAuth);
                return sessionAuth;
            }
        }
    }

    public class SessionAuth : Singleton<SessionAuth>
    {
        const int MAX_SESSION_OBJS_POOL_SIZE = 1000;
        const int MAX_SESSION_LIFE_TIME_SEC = 60 * 5;

        List<Session> mSessionObjPool = new List<Session>();
        SimpleRNG.SimpleRNGCore mRandomTools = new SimpleRNG.SimpleRNGCore();

        public SessionAuth()
        {
            mRandomTools.SetSeedFromSystemTime();
        }

        Session _TakeSessionObjFromPool()
        {
            if (mSessionObjPool.Count > 0)
            {
                var result = mSessionObjPool[mSessionObjPool.Count - 1];
                result.ContextObj = null;
                result.ValiditySec = 0;
                result.StartTime = DateTime.UtcNow;
                mSessionObjPool.RemoveAt(mSessionObjPool.Count - 1);
                return result;
            }
            else
            {
                return new Session();
            }
        }

        void _RecyclingSessionObjToPool(Session sessionObj)
        {
            if (mSessionObjPool.Count < MAX_SESSION_OBJS_POOL_SIZE)
                mSessionObjPool.Add(sessionObj);
        }

        Dictionary<string, Session> mTokenInfo = new Dictionary<string, Session>();

        byte[] mBytesBuff = null;
        object _locker = new object();
        string _GenerateTokenString(uint ctxSeed)
        {
            string result = "";
            uint randA = mRandomTools.GetUint();
            uint randB = mRandomTools.GetUint();

            ulong rand = randA ^ ctxSeed;
            rand = rand << 32 | randB;

            byte[] bytes1 = BitConverter.GetBytes(rand);

            randA = mRandomTools.GetUint();
            randB = mRandomTools.GetUint();

            rand = randA ^ ctxSeed;
            rand = rand << 32 | randB;
            byte[] bytes2 = BitConverter.GetBytes(rand);

            lock (_locker)
            {
                if (mBytesBuff == null || mBytesBuff.Length < bytes1.Length + bytes2.Length)
                    mBytesBuff = new byte[bytes1.Length + bytes2.Length];

                bytes1.CopyTo(mBytesBuff, 0);
                bytes2.CopyTo(mBytesBuff, bytes1.Length);

                result += Convert.ToBase64String(mBytesBuff);
            }
            result = result.Replace('+','-');
            result = result.Replace('/','_');
            return result.TrimEnd('=');
        }

        void _RemoveSession(string token)
        {
            if (mTokenInfo.ContainsKey(token))
            {
                var sessionObj = mTokenInfo[token];
                mTokenInfo.Remove(token);
                sessionObj.ContextObj = null;
                _RecyclingSessionObjToPool(sessionObj);
            }
        }

        public string GenerateToken(object authInfo)
        {
            return GenerateToken(authInfo, MAX_SESSION_LIFE_TIME_SEC);
        }

        public string GenerateToken(object authInfo, long validitySec)
        {
            lock (this)
            {
                string token = _GenerateTokenString((uint)authInfo.GetHashCode());
                while (mTokenInfo.ContainsKey(token))
                    token = _GenerateTokenString((uint)authInfo.GetHashCode());

                var newSession = _TakeSessionObjFromPool();
                newSession.StartTime = DateTime.UtcNow;
                newSession.ValidityStartTime = DateTime.UtcNow;
                newSession.ContextObj = authInfo;
                newSession.ValiditySec = validitySec;
                mTokenInfo.Add(token, newSession);

                return token;
            }
        }

        public void ResetAuthContext(string token, object authInfo)
        {
            lock (this)
            {
                if (mTokenInfo.ContainsKey(token))
                {
                    var sessionObj = mTokenInfo[token];
                    sessionObj.ContextObj = authInfo;
                }
            }
        }

        public bool IsSessionAlive(string token)
        {
            lock (this)
            {
                if (mTokenInfo.ContainsKey(token))
                {
                    var sessionObj = mTokenInfo[token];
                    return sessionObj.IsInValidityTime;
                }
            }

            return false;
        }

        public int QuerySessionLifeTime(string token)
        {
            lock (this)
            {
                if (mTokenInfo.ContainsKey(token))
                {
                    var sessionObj = mTokenInfo[token];
                    return sessionObj.ValidityLease;
                }
            }

            return 0;
        }

        public object ReConsturctAuthInfo(string token, bool renewValidityTime = true)
        {
            lock (this)
            {
                if (mTokenInfo.ContainsKey(token))
                {
                    var sessionObj = mTokenInfo[token];
                    // 检查时效性
                    if (sessionObj.IsInValidityTime)
                    {
                        if (renewValidityTime)
                            sessionObj.ValidityStartTime = DateTime.UtcNow;
                        return sessionObj.ContextObj;
                    }
                    _RemoveSession(token);
                }
            }

            return null;
        }

        public void ResetSessionValidity(string token, long validitySec)
        {
            lock (this)
            {
                if (mTokenInfo.ContainsKey(token))
                {
                    var sessionObj = mTokenInfo[token];
                    sessionObj.ValidityStartTime = DateTime.UtcNow;
                    sessionObj.ValiditySec = validitySec;
                }
            }
        }

        public void ReleaseSession(string token)
        {
            lock (this) _RemoveSession(token);
        }

        public void GCSessions()
        {
            lock (this)
            {
                List<string> deleting = new List<string>();
                foreach (var kv in mTokenInfo)
                {
                    var sessionObj = kv.Value;
                    if (sessionObj.IsInValidityTime)
                        continue;
                    deleting.Add(kv.Key);
                }

                for (int i = 0; i < deleting.Count; i++)
                {
                    var token = deleting[i];
                    var sessionObj = mTokenInfo[token];
                    mTokenInfo.Remove(token);
                    sessionObj.ContextObj = null;
                    _RecyclingSessionObjToPool(sessionObj);
                }
            }
        }
    }
}