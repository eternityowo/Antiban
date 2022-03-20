using System.Collections.Generic;
using System;
using System.Linq;

namespace Antiban
{
    public class Antiban
    {
        private class Bucket
        {
            private readonly SortedDictionary<DateTime, AntibanResult> _result;

            private DateTime _nextTime0 = DateTime.MinValue;
            private DateTime _nextTime1 = DateTime.MinValue;

            public Bucket(SortedDictionary<DateTime, AntibanResult> result)
            {
                _result = result;
            }

            public void Add(EventMessage eventMessage)
            {
                if (eventMessage.Priority == 0)
                {
                    if (eventMessage.DateTime - _nextTime0 < MIN_01)
                    {
                        _nextTime0 = _nextTime0 + MIN_01;
                    }
                    else
                    {
                        _nextTime0 = eventMessage.DateTime;
                    }

                    _result.Add(_nextTime0, new AntibanResult() { EventMessageId = eventMessage.Id, SentDateTime = _nextTime0 });
                }
                else
                {
                    if (eventMessage.DateTime - _nextTime1 < DAY_01)
                    {
                        _nextTime1 = _nextTime1 + DAY_01;
                    }
                    else
                    {
                        if (eventMessage.DateTime - _nextTime0 < MIN_01)
                        {
                            _nextTime1 = _nextTime0 + MIN_01;
                            _nextTime0 += MIN_01;
                        }
                        else
                        {
                            _nextTime1 = eventMessage.DateTime;
                            _nextTime0 = eventMessage.DateTime;
                        }
                    }

                    _result.Add(_nextTime1, new AntibanResult() { EventMessageId = eventMessage.Id, SentDateTime = _nextTime1 });
                }
            }
        }

        private Dictionary<string, Bucket> _phoneDic = new Dictionary<string, Bucket>();

        private (string phone, DateTime time) _timeBetweenPhone = ("0", DateTime.MinValue);

        private SortedDictionary<DateTime, AntibanResult> _result = new SortedDictionary<DateTime, AntibanResult>();

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
                _phoneDic.Add(phone, new Bucket(_result));
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

            _phoneDic[phone].Add(eventMessage);
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
