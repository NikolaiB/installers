#!/bin/bash

( cd root.rh && find . | cpio -o --format odc --owner 0:80 | gzip -c ) > flat/rh.pkg/Payload
( cd root.mh && find . | cpio -o --format odc --owner 0:80 | gzip -c ) > flat/mh.pkg/Payload
( cd scripts.rh && find . | cpio -o --format odc --owner 0:80 | gzip -c ) > flat/rh.pkg/Scripts
( cd scripts.mh && find . | cpio -o --format odc --owner 0:80 | gzip -c ) > flat/mh.pkg/Scripts
mkbom -u 0 -g 80 root.rh flat/rh.pkg/Bom
mkbom -u 0 -g 80 root.mh flat/mh.pkg/Bom
( cd flat && xar --compression none -cf "$@" * )
