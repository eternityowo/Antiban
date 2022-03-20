using System.Collections.Generic;
using System;
using System.Linq;

namespace Antiban
{
    public class Antiban
    {
        private class Bucket
        {
            public DateTime Time0 { get; set; } = DateTime.MinValue;
            public DateTime Time1 { get; set; } = DateTime.MinValue;
        }

        private Dictionary<string, Bucket> _phoneDic = new Dictionary<string, Bucket>();

        private SortedDictionary<DateTime, AntibanResult> _result = new SortedDictionary<DateTime, AntibanResult>();

        private (string phone, DateTime time) _timeBetweenPhone = ("0", DateTime.MinValue);

        private static readonly TimeSpan SEC_10 = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan MIN_01 = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DAY_01 = TimeSpan.FromDays(1);

        /// <summary>
        /// Добавление сообщений в систему, для обработки порядка сообщений
        /// </summary>
        /// <param name="eventMessage"></param>
        public void PushEventMessage(EventMessage eventMessage)
        {
            var phone = eventMessage.Phone;

            if (!_phoneDic.ContainsKey(phone))
            {
                _phoneDic.Add(phone, new Bucket());
            }

            if (_timeBetweenPhone.phone != phone)
            {
                if (eventMessage.DateTime - _timeBetweenPhone.time < SEC_10)
                {
                    eventMessage.DateTime = _timeBetweenPhone.time + SEC_10;

                    if (_result.ContainsKey(eventMessage.DateTime))
                    {
                        eventMessage.DateTime += SEC_10;
                    }
                }
                _timeBetweenPhone = (phone, eventMessage.DateTime);
            }

            var bucket = _phoneDic[phone];

            if (eventMessage.Priority == 0)
            {
                if (eventMessage.DateTime - bucket.Time0 < MIN_01)
                {
                    bucket.Time0 = bucket.Time0 + MIN_01;
                }
                else
                {
                    bucket.Time0 = eventMessage.DateTime;
                }

                var antibanResult = new AntibanResult() { EventMessageId = eventMessage.Id, SentDateTime = bucket.Time0 };
                _result.Add(bucket.Time0, antibanResult);
            }
            else
            {
                if (eventMessage.DateTime - bucket.Time1 < DAY_01)
                {
                    bucket.Time1 = bucket.Time1 + DAY_01;
                }
                else
                {
                    if (eventMessage.DateTime - bucket.Time0 < MIN_01)
                    {
                        bucket.Time1 = bucket.Time0 + MIN_01;
                        bucket.Time0 += MIN_01;
                    }
                    else
                    {
                        bucket.Time1 = eventMessage.DateTime;
                        bucket.Time0 = eventMessage.DateTime;
                    }
                }

                var antibanResult = new AntibanResult() { EventMessageId = eventMessage.Id, SentDateTime = bucket.Time1 };
                _result.Add(bucket.Time1, antibanResult);
            }
        }

        /// <summary>
        /// Вовзращает порядок отправок сообщений
        /// </summary>
        /// <returns></returns>
        public List<AntibanResult> GetResult()
        {
            return _result.Select(x => x.Value).ToList();
        }
    }
}
