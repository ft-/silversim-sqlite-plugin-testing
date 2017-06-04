#!/bin/sh

case "$1" in

  'clean')

    mono Prebuild/Prebuild.exe /clean

  ;;


  'autoclean')

    echo y|mono Prebuild/Prebuild.exe /clean

  ;;


  *)

    mono Prebuild/Prebuild.exe /target vs2015

  ;;

esac
