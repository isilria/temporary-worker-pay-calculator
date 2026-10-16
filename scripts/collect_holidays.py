"""Collect public holiday names and dates. Never put the service key in output."""
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET

ENDPOINT = 'https://apis.data.go.kr/B090041/openapi/service/SpcdeInfoService/getRestDeInfo'
SOURCE = 'https://www.data.go.kr/data/15012690/openapi.do'

def fingerprint(years):
    text = ''.join(str(y['year']) + '\n' + ''.join(h['date'] + '|' + h['name'] + '\n' for h in sorted(y['holidays'], key=lambda x:x['date'])) for y in sorted(years, key=lambda x:x['year']))
    return hashlib.sha256(text.encode('utf-8')).hexdigest()

def validate_year(year):
    value = year['year']
    holidays = year['holidays']
    if not 2020 <= value <= 2050 or not 10 <= len(holidays) <= 50:
        raise ValueError('Invalid or incomplete holiday year')
    dates = set()
    for holiday in holidays:
        date = dt.date.fromisoformat(holiday['date'])
        name = holiday['name']
        if date.year != value or date.isoformat() != holiday['date'] or holiday['date'] in dates:
            raise ValueError('Duplicate date or wrong year')
        if not isinstance(name, str) or not name.strip() or len(name)>100 or any(ord(c)<32 or c=='|' for c in name):
            raise ValueError('Invalid holiday name')
        dates.add(holiday['date'])
    if f'{value}-01-01' not in dates or f'{value}-12-25' not in dates:
        raise ValueError('Required reference holidays are missing')

def fetch_year(year, credential):
    query = urllib.parse.urlencode({'serviceKey':urllib.parse.unquote(credential), 'solYear':year, 'numOfRows':100, 'pageNo':1})
    request = urllib.request.Request(ENDPOINT+'?'+query, headers={'User-Agent':'ShortPay-HolidayCollector/1.0'})
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *args, **kwargs):
            raise ValueError('API redirect rejected')
    with urllib.request.build_opener(NoRedirect).open(request, timeout=30) as response:
        raw = response.read(262145)
    if len(raw)>262144:
        raise ValueError('Oversized API response')
    root = ET.fromstring(raw)
    if root.findtext('./header/resultCode') != '00':
        raise ValueError('API did not report success')
    items = root.findall('./body/items/item')
    if int(root.findtext('./body/totalCount', '-1')) != len(items):
        raise ValueError('Incomplete API response')
    grouped = {}
    for item in items:
        if item.findtext('isHoliday') != 'Y':
            continue
        value = item.findtext('locdate', '')
        date = dt.datetime.strptime(value,'%Y%m%d').date().isoformat()
        grouped.setdefault(date,set()).add(item.findtext('dateName','').strip())
    result = {'year':year,'holidays':[{'date':date,'name':' / '.join(sorted(names))} for date,names in sorted(grouped.items())]}
    validate_year(result)
    return result

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--credential-file',type=Path)
    parser.add_argument('--output',type=Path,default=Path('data/holidays.json'))
    parser.add_argument('--years',type=int,nargs='+')
    args=parser.parse_args()
    credential=os.environ.get('HOLIDAY_API_KEY','').strip()
    if args.credential_file:
        raw=args.credential_file.read_text(encoding='utf-8-sig')
        remaining=re.sub(r'https?://[^\s<>"\']+',' ',raw)
        candidates=re.findall(r'[A-Za-z0-9%+/=_\-]{30,}',remaining)
        if len(candidates)!=1: raise ValueError('Expected one credential in the local file')
        credential=candidates[0]
    if not credential: raise ValueError('HOLIDAY_API_KEY is missing')
    current=dt.datetime.now(dt.timezone(dt.timedelta(hours=9))).year
    years=sorted(set(args.years or [current-1,current,current+1]))
    existing={}
    if args.output.exists():
        previous=json.loads(args.output.read_text(encoding='utf-8-sig'))
        for year in previous['years']:
            validate_year(year)
            existing[year['year']]=year
    # Do not replace anything unless every requested year is complete and valid.
    for year in years:
        existing[year]=fetch_year(year,credential)
    data={'schemaVersion':1,'sourceUrl':SOURCE,'fetchedAtUtc':dt.datetime.now(dt.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'),'years':[existing[year] for year in sorted(existing)]}
    data['contentVersion']=fingerprint(data['years'])
    args.output.parent.mkdir(parents=True,exist_ok=True)
    temporary=args.output.with_suffix('.json.tmp')
    temporary.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    os.replace(temporary,args.output)
    print(json.dumps({'success':True,'years':years,'records':{y['year']:len(y['holidays']) for y in data['years']},'contentVersion':data['contentVersion']},ensure_ascii=False))

if __name__=='__main__':
    try:
        main()
    except Exception as error:
        # Request URLs contain a secret. Do not print exceptions or tracebacks.
        print('Holiday collection failed ('+type(error).__name__+'). Existing data was preserved.',file=sys.stderr)
        sys.exit(1)
