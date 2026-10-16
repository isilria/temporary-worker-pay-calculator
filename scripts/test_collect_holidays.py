import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import collect_holidays as collector

DATA=json.loads((Path(__file__).resolve().parents[1]/'data/holidays.json').read_text(encoding='utf-8-sig'))

class CollectorTests(unittest.TestCase):
    def test_initial_fingerprint(self):
        self.assertEqual(collector.fingerprint(DATA['years']),DATA['contentVersion'])

    def test_year_is_complete(self):
        for year in DATA['years']: collector.validate_year(year)

    def test_empty_year(self):
        with self.assertRaises(ValueError):collector.validate_year({'year':2026,'holidays':[]})

    def test_partial_year(self):
        year=copy.deepcopy(DATA['years'][1]);year['holidays']=[d for d in year['holidays'] if d['date']!='2026-01-01']
        with self.assertRaises(ValueError):collector.validate_year(year)

    def test_duplicate(self):
        year=copy.deepcopy(DATA['years'][1]);year['holidays'].append(year['holidays'][0])
        with self.assertRaises(ValueError):collector.validate_year(year)

    def test_wrong_year(self):
        year=copy.deepcopy(DATA['years'][1]);year['holidays'][0]['date']='2027-01-01'
        with self.assertRaises(ValueError):collector.validate_year(year)

    def test_bad_name(self):
        year=copy.deepcopy(DATA['years'][1]);year['holidays'][0]['name']='bad\nname'
        with self.assertRaises(ValueError):collector.validate_year(year)

    def test_stable_version_independent_of_order(self):
        years=copy.deepcopy(DATA['years']);years.reverse()
        for year in years:year['holidays'].reverse()
        self.assertEqual(collector.fingerprint(years),DATA['contentVersion'])

    def test_name_change_has_new_version(self):
        years=copy.deepcopy(DATA['years']);years[0]['holidays'][0]['name']='검증용 명칭'
        self.assertNotEqual(collector.fingerprint(years),DATA['contentVersion'])

    def test_failure_preserves_existing_file(self):
        with tempfile.TemporaryDirectory() as folder:
            output=Path(folder)/'holidays.json';original=json.dumps(DATA);output.write_text(original,encoding='utf-8')
            with patch('sys.argv',['collect','--output',str(output),'--years','2026','2027']),patch.dict('os.environ',{'HOLIDAY_API_KEY':'test-only'}),patch.object(collector,'fetch_year',side_effect=[DATA['years'][1],ValueError('incomplete')]):
                with self.assertRaises(ValueError):collector.main()
            self.assertEqual(output.read_text(encoding='utf-8'),original)

    def test_keeps_old_year_and_does_not_write_credential(self):
        with tempfile.TemporaryDirectory() as folder:
            output=Path(folder)/'holidays.json';output.write_text(json.dumps(DATA),encoding='utf-8')
            with patch('sys.argv',['collect','--output',str(output),'--years','2026']),patch.dict('os.environ',{'HOLIDAY_API_KEY':'test-secret-never-publish'}),patch.object(collector,'fetch_year',return_value=DATA['years'][1]),contextlib.redirect_stdout(io.StringIO()) as printed:
                collector.main()
            self.assertEqual(len(json.loads(output.read_text(encoding='utf-8'))['years']),len(DATA['years']))
            self.assertNotIn('test-secret-never-publish',output.read_text(encoding='utf-8')+printed.getvalue())

    def test_api_error_and_incomplete_response(self):
        class Response:
            def __init__(self,body):self.body=body
            def __enter__(self):return self
            def __exit__(self,*args):pass
            def read(self,*args):return self.body
        for body in [b'<response><header><resultCode>30</resultCode></header></response>',b'<response><header><resultCode>00</resultCode></header><body><totalCount>2</totalCount><items/></body></response>']:
            with patch.object(collector.urllib.request,'build_opener') as opener:
                opener.return_value.open.return_value=Response(body)
                with self.assertRaises(ValueError):collector.fetch_year(2026,'test-only')

    def test_five_year_window_and_year_rollover(self):
        self.assertEqual(collector.default_years(2026),[2026,2027,2028,2029,2030])
        self.assertEqual(collector.default_years(2027),[2027,2028,2029,2030,2031])

    def run_future_collection(self,output,fetch):
        with patch('sys.argv',['collect','--output',str(output),'--years','2026','2027','2028','2029','2030']),patch.dict('os.environ',{'HOLIDAY_API_KEY':'test-only'}),patch.object(collector,'fetch_year',side_effect=fetch),contextlib.redirect_stdout(io.StringIO()):
            collector.main()

    def future_response(self,year,key):
        if year in (2029,2030):raise collector.UnavailableYear()
        if year==2028:
            item=copy.deepcopy(DATA['years'][1]);item['year']=2028
            for day in item['holidays']:day['date']='2028'+day['date'][4:]
            return item
        return copy.deepcopy(next(y for y in DATA['years'] if y['year']==year))

    def test_empty_future_years_do_not_block_available_years(self):
        with tempfile.TemporaryDirectory() as folder:
            output=Path(folder)/'holidays.json';output.write_text(json.dumps(DATA),encoding='utf-8')
            self.run_future_collection(output,self.future_response)
            result=json.loads(output.read_text(encoding='utf-8'))
            self.assertEqual([y['year'] for y in result['years']],[2025,2026,2027,2028])
            self.assertEqual(result['requestedYears'],[2026,2027,2028,2029,2030])
            self.assertEqual(result['unavailableYears'],[2029,2030])
            self.assertEqual(result['contentVersion'],collector.fingerprint(result['years']))

    def test_later_future_publication_is_saved(self):
        with tempfile.TemporaryDirectory() as folder:
            output=Path(folder)/'holidays.json';output.write_text(json.dumps(DATA),encoding='utf-8')
            self.run_future_collection(output,self.future_response)
            def later(year,key):
                if year==2029:
                    item=self.future_response(2028,key);item['year']=2029
                    for day in item['holidays']:day['date']='2029'+day['date'][4:]
                    return item
                return self.future_response(year,key)
            self.run_future_collection(output,later)
            result=json.loads(output.read_text(encoding='utf-8'))
            self.assertIn(2029,[y['year'] for y in result['years']])
            self.assertEqual(result['unavailableYears'],[2030])

    def test_future_network_failure_preserves_file(self):
        with tempfile.TemporaryDirectory() as folder:
            output=Path(folder)/'holidays.json';original=json.dumps(DATA);output.write_text(original,encoding='utf-8')
            def fetch(year,key):
                if year==2030:raise collector.urllib.error.URLError('test offline')
                return self.future_response(year,key)
            with self.assertRaises(collector.urllib.error.URLError):self.run_future_collection(output,fetch)
            self.assertEqual(output.read_text(encoding='utf-8'),original)

    def test_previously_saved_year_disappearing_preserves_file(self):
        with tempfile.TemporaryDirectory() as folder:
            output=Path(folder)/'holidays.json';original=json.dumps(DATA);output.write_text(original,encoding='utf-8')
            def fetch(year,key):
                if year==2027:raise collector.UnavailableYear()
                return self.future_response(year,key)
            with self.assertRaises(ValueError):self.run_future_collection(output,fetch)
            self.assertEqual(output.read_text(encoding='utf-8'),original)

if __name__=='__main__':unittest.main()
