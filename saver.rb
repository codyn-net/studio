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
		
		def self.ensure_ids(objects, startid = 0)
			objects.each do |obj|
				next unless obj.properties.include?(:id)
				
				if !obj.property_set?(:id) || obj.get_property(:id) == nil || obj.get_property(:id).empty?
					obj.set_property(:id, Time.now.to_i + startid)
					startid += 1
				end
				
				if obj.is_a?(Components::Group)
					startid = ensure_ids(obj.children, startid)
				end
			end
		end
	end
end
