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
            self.assertEqual(len(json.loads(output.read_text(encoding='utf-8'))['years']),3)
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

if __name__=='__main__':unittest.main()
