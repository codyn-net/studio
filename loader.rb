require 'rexml/document'
require 'rexml/element'
require 'serialize/serialize'

module Cpg
	class Loader
		def self.load(contents)
			doc = REXML::Document.new(contents)
		
			raise('Could not parse xml') unless (doc and doc.root)
			Serialize.reset
			return Serialize.from_xml(doc.root)
		end
	end
end
