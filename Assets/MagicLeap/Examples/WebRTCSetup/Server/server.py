#!/usr/bin/env python3

import logging
import json
from collections import defaultdict
import sys

from bottle import route, run, request, response, hook, static_file, redirect

logger = logging.getLogger('server')

next_client_id = 1
created_offers = {}
created_answers = {}
ices = defaultdict(list)

@hook('after_request')
def enable_cors():
    response.headers['Access-Control-Allow-Origin'] = '*'

@route('/login', method='POST')
def login():
    global next_client_id
    res = next_client_id
    logger.info('Logged in user from %s as %s', request.environ.get('REMOTE_ADDR'), res)
    next_client_id += 1
    return str(res)

@route('/logout/<id:int>', method='POST')
def logout(id):
    logger.info('User %s logged out', id)

    if id in created_offers:
        del created_offers[id]
    if id in ices:
        del ices[id]
    for answer in created_answers:
        if id in created_answers[answer]:
            del created_answers[answer][id]

@route('/offers')
def offers():
    return created_offers

@route('/answer/<id:int>')
def answer(id):
    return created_answers.get(id, {})

@route('/post_offer/<id:int>', method='POST')
def post_offer(id):
    logger.info('User %s posted offer', id)
    created_offers[id] = json.loads(request.body.read().decode('utf-8'))

@route('/post_answer/<from_id:int>/<to_id:int>', method='POST')
def post_answer(from_id, to_id):
    del created_offers[to_id]
    logger.info('User %s posted answer to user %s', from_id, to_id)
    created_answers[to_id] = dict(id=from_id, answer=json.loads(request.body.read().decode('utf-8')))

@route('/post_ice/<id:int>', method='POST')
def post_ice(id):
    logger.info('User %s posted ICE', id)
    ices[id].append(json.loads(request.body.read().decode('utf-8')))

@route('/consume_ices/<id:int>', method='POST')
def consume_ices(id):
    res = ices[id]
    ices[id] = []
    if len(res) > 0:
        logger.info('Someone read ICEs from user %s', id)
    return dict(ices=res)

@route('/client/<filename>')
def client(filename):
    return static_file(filename, root='../Browser')

@route('/')
def root():
    redirect('/client/index.html')

logging.basicConfig(level=logging.INFO)
port = sys.argv[1] if len(sys.argv) > 1 else 8080

run(host='0.0.0.0', port=port)
