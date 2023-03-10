;;; column_writer.clj -- part of the pretty printer for Clojure


;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

;; Author: Tom Faulhaber
;; April 3, 2009
;; Revised to use proxy instead of gen-class April 2010

;; This module implements a column-aware wrapper around an instance of java.io.Writer

(in-ns 'clojure.pprint)

(import [clojure.lang IDeref]
        [System.IO TextWriter])                          ;;;  java.io Writer    All instances of Writer replaced by TextWriter

(def ^:dynamic ^{:private true} *default-page-width* 72)

(defn- get-field [^TextWriter this sym]
  (sym @@this))

(defn- set-field [^TextWriter this sym new-val] 
  (alter @this assoc sym new-val))

(defn- get-column [this]
  (get-field this :cur))

(defn- get-line [this]
  (get-field this :line))

(defn- get-max-column [this]
  (get-field this :max))

(defn- set-max-column [this new-max]
  (dosync (set-field this :max new-max))
  nil)

(defn- get-writer [this]
  (get-field this :base))

(defn- c-write-char [^TextWriter this c]  (let [c (int c)]   ;;; in place of ^Integer
  (dosync (if (= c (int \newline))
	    (do
              (set-field this :cur 0)
              (set-field this :line (inc (get-field this :line))))
	    (set-field this :cur (inc (get-field this :cur)))))
  (.Write ^TextWriter (get-field this :base) (char c))) )
  
(defn- column-writer   
  ([writer] (column-writer writer *default-page-width*))
  ([^TextWriter writer max-columns]
     (let [fields (ref {:max max-columns, :cur 0, :line 0 :base writer})]
       (proxy [TextWriter IDeref] []
         (deref [] fields)

         (Flush []
           (.Flush writer))
         
         (Write
           ([^chars cbuf off len]
            (let [off (int off)
                  len (int len)
                  ^TextWriter writer (get-field this :base)]
              (.Write writer cbuf off len)))
           ([^String x]
            (let [^String s x
                  nl (.LastIndexOf s \newline)]
              (dosync (if (neg? nl)
                        (set-field this :cur (+ (get-field this :cur) (count s)))
                        (do
                          (set-field this :cur (- (count s) nl 1))
                          (set-field this :line (+ (get-field this :line)
                                                   (count (filter #(= % \newline) s)))))))
              (.Write ^TextWriter (get-field this :base) s)))
           ([^Char x] (.Write writer ^Char x))
           ([^Int32 x] (c-write-char this x))
           ([^Int64 x] (c-write-char this x)))))))