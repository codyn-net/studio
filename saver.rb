require 'rexml/document'
require 'rexml/element'

module Cpg
	class Saver
		def self.save(cpg)
			doc = REXML::Document.new
			doc << REXML::XMLDecl.new

			doc << cpg.to_xml
			doc
		end
	end
end
